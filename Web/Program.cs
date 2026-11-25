using Application.Interfaces.Auth;
using Infrastructure.Data;
using Infrastructure.Data.Seed;
using IoC;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Shared.Constantes;
using Shared.Responses;
using System.Security.Claims;
using System.Text;
using Web.Middleware;

var builder = WebApplication.CreateBuilder(args);

// ----- Services -----
builder.Services.AddControllers()
    // Los errores de model binding y DataAnnotations los corta [ApiController] ANTES de llegar al
    // action, así que nunca pasan por ExceptionHandlingMiddleware: sin esto saldrían como
    // ValidationProblemDetails y el front recibiría, solo para este caso, un shape distinto al
    // BaseResponse de todos los demás 400.
    .ConfigureApiBehaviorOptions(options =>
    {
        options.InvalidModelStateResponseFactory = context =>
        {
            var campos = context.ModelState
                .Where(e => e.Value is { Errors.Count: > 0 })
                // El binder de JSON prefija la ruta con "$."; el nombre pelado es lo que le sirve al front.
                .Select(e => e.Key.StartsWith("$.", StringComparison.Ordinal) ? e.Key[2..] : e.Key)
                .Where(nombre => !string.IsNullOrWhiteSpace(nombre))
                .Distinct()
                .ToList();

            // El detalle por campo viene en inglés desde DataAnnotations: se informan los campos
            // en falta y el mensaje queda en español, como el resto de la API.
            var mensaje = campos.Count > 0
                ? $"Datos inválidos: {string.Join(", ", campos)}."
                : "Datos inválidos.";

            return new BadRequestObjectResult(BaseResponse<string>.Fail(mensaje, toast: true));
        };
    });
builder.Services.AddEndpointsApiExplorer();

// --- Swagger + JWT ---
builder.Services.AddSwaggerGen(options =>
{
    options.EnableAnnotations();
    options.SwaggerDoc("v1", new OpenApiInfo { Title = "Turnero API", Version = "v1" });
    options.CustomSchemaIds(type => type.FullName);

    // Botón "Authorize": pegar solo el token JWT (sin "Bearer ").
    var jwtScheme = new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Pegá solo el token JWT (sin el prefijo 'Bearer ').",
        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
    };
    options.AddSecurityDefinition("Bearer", jwtScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { jwtScheme, Array.Empty<string>() }
    });
});

// --- CORS ---
const string DevCorsPolicy = "DevCorsPolicy";
var allowedOriginsRaw = builder.Configuration["CorsSettings:AllowedOrigin"]
    ?? Environment.GetEnvironmentVariable("CorsSettings__AllowedOrigin")
    ?? throw new Exception("CorsSettings:AllowedOrigin missing!");

var allowedOrigins = allowedOriginsRaw
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyMethod()
              .AllowAnyHeader()
              .AllowCredentials();
    });
});

// --- EF Core: DbContext (PostgreSQL) ---
builder.Services.AddDbContext<TurneroContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TurneroEntities"))
);

// --- HttpContext + servicios de la solución ---
builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddProjectServices();

// --- JWT Authentication ---
var jwtSection = builder.Configuration.GetSection("JwtSettings");
var secret = jwtSection["Key"]
    ?? Environment.GetEnvironmentVariable("JwtSettings__Key")
    ?? throw new Exception("JwtSettings:Key missing!");
var issuer = jwtSection["Issuer"]
    ?? Environment.GetEnvironmentVariable("JwtSettings__Issuer")
    ?? throw new Exception("JwtSettings:Issuer missing!");
var audience = jwtSection["Audience"]
    ?? Environment.GetEnvironmentVariable("JwtSettings__Audience")
    ?? throw new Exception("JwtSettings:Audience missing!");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateLifetime = true,
            RequireExpirationTime = true,
            ClockSkew = TimeSpan.FromMinutes(2),
            RoleClaimType = ClaimTypes.Role
        };
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = ctx =>
            {
                if (ctx.Request.Cookies.TryGetValue("access_token", out var cookieToken))
                    ctx.Token = cookieToken;
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Plano plataforma vs plano negocio (según el claim tipo_acceso del JWT).
    options.AddPolicy("Superadmin", p => p.RequireClaim(CteSeguridad.ClaimTipoAcceso, CteSeguridad.TipoAccesoPlataforma));
    options.AddPolicy("Tenant", p => p.RequireClaim(CteSeguridad.ClaimTipoAcceso, CteSeguridad.TipoAccesoTenant));
});

var app = builder.Build();

// Aplica las migraciones EF pendientes al arrancar cuando RUN_MIGRATIONS=true (prod/Docker).
// Crea el esquema completo en una BD nueva; en dev se sigue usando `dotnet ef database update`.
if (Environment.GetEnvironmentVariable("RUN_MIGRATIONS") == "true")
{
    using var migrationScope = app.Services.CreateScope();
    await migrationScope.ServiceProvider.GetRequiredService<TurneroContext>().Database.MigrateAsync();
}

// Warm-up del modelo EF + seed inicial (superadmin) si la base está disponible.
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<TurneroContext>();
        _ = context.Model;

        var passwordService = services.GetRequiredService<IPasswordService>();
        await SeedData.EnsureSeedAsync(context, passwordService, app.Configuration);

        if (app.Environment.IsDevelopment())
            await SeedData.EnsureDevSeedAsync(context, passwordService);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILoggerFactory>().CreateLogger("Startup");

        if (app.Environment.IsDevelopment())
        {
            // En dev es común levantar sin la base migrada todavía; no se interrumpe el arranque.
            logger.LogWarning(ex, "Warm-up/seed omitido al arrancar");
        }
        else
        {
            // En producción, si el seed (superadmin) falla, mejor no levantar en silencio.
            logger.LogError(ex, "Warm-up/seed falló al arrancar");
            throw;
        }
    }
}

var swaggerHabilitado = Environment.GetEnvironmentVariable("ENABLE_SWAGGER") == "true"
                        || app.Environment.IsDevelopment();

if (swaggerHabilitado)
{
    // Fuera de desarrollo, /swagger va detrás de basic auth (SwaggerAuth:Username/Password).
    if (!app.Environment.IsDevelopment())
        app.UseMiddleware<SwaggerBasicAuthMiddleware>();

    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Turnero API v1");
        options.RoutePrefix = "swagger";

        // Que Swagger mande las cookies (queda autenticado tras el login, sin pegar el Bearer)...
        options.ConfigObject.AdditionalItems["withCredentials"] = true;
        // ...y que adjunte solo el X-CSRF-Token leyéndolo de la cookie csrf_token.
        options.UseRequestInterceptor(
            "(req) => { const m = document.cookie.match(/(?:^|; )csrf_token=([^;]+)/); if (m) { req.headers['X-CSRF-Token'] = decodeURIComponent(m[1]); } return req; }");
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseRouting();
app.UseCors(DevCorsPolicy);
app.UseMiddleware<CsrfMiddleware>();
app.UseAuthentication();
app.UseAuthorization();

// Bloqueo por cuenta suspendida y por términos sin aceptar (necesitan los claims; van tras la auth).
app.UseMiddleware<TenantSuspendidoMiddleware>();
app.UseMiddleware<TerminosPendientesMiddleware>();

app.MapControllers();

// El back no sirve front (no hay wwwroot): sin esto la raíz devuelve un 404 pelado del router.
// Con Swagger habilitado, / lleva a la UI; si no, responde el wrapper estándar y sirve de healthcheck.
app.MapGet("/", () => swaggerHabilitado
    ? Results.Redirect("/swagger/index.html")
    : Results.Ok(BaseResponse<string>.Ok("Turnero API", "API operativa")));

var port = Environment.GetEnvironmentVariable("PORT") ?? "53714";
app.Run($"http://0.0.0.0:{port}");
