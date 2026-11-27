# Build multi-stage: el SDK compila/publica, la imagen runtime solo corre Web.dll.
# Contexto de build = raiz del repo (donde esta Turnero.sln).
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Copiar solo los .csproj primero para cachear el restore mientras no cambien las dependencias.
COPY Domain/Domain.csproj Domain/
COPY Shared/Shared.csproj Shared/
COPY Application/Application.csproj Application/
COPY Infrastructure/Infrastructure.csproj Infrastructure/
COPY IoC/IoC.csproj IoC/
COPY Web/Web.csproj Web/
RUN dotnet restore Web/Web.csproj

# Copiar el resto del codigo y publicar el proyecto Web (arrastra Domain/Shared/Application/Infrastructure/IoC).
COPY . .
RUN dotnet publish Web/Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_ENVIRONMENT=Production
# El puerto real lo define la env var PORT (Program.cs escucha en 0.0.0.0:$PORT): 5004 en docker-compose.prod.yml.
EXPOSE 5004
ENTRYPOINT ["dotnet", "Web.dll"]
