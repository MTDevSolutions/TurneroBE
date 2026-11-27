# TurneroBE

Backend del turnero para profesionales de la salud (.NET 10, PostgreSQL, multi-tenant).
Parte de [BaseTenant](https://github.com/MTDevSolutions/BaseTenant) con las mejoras de CellFixBE.
El front está en [TurneroFE](https://github.com/MTDevSolutions/TurneroFE).

Ver [CLAUDE.md](./CLAUDE.md) para arquitectura y convenciones.

## Probarlo en tu compu

Necesitás Docker, el SDK de .NET 10 y Node 20.19 o superior.

**1. Base y API** (en este repo, rama `dev`):

```bash
docker compose up -d                          # Postgres en el puerto 5436
RUN_MIGRATIONS=true dotnet run --project Web  # API en http://localhost:5080/swagger
```

En PowerShell: `$env:RUN_MIGRATIONS="true"; dotnet run --project Web`.

La primera vez crea las tablas y carga datos de ejemplo (solo en Development):

| Cuenta | Email | Password | Qué tiene |
|---|---|---|---|
| Psicología | `psico@demo.local` | `Demo1234!` | Términos aceptados, 8 pacientes y una semana de turnos alrededor del día en que se creó |
| Fonoaudiología | `fono@demo.local` | `Demo1234!` | Sin datos y con los términos pendientes (para ver esa pantalla) |
| Superadmin | `admin@turnero.local` | `Admin1234!` | Plataforma (`/api/auth/superadmin/login`, por Swagger) |

**2. Front** (en TurneroFE, rama `dev`):

```bash
npm install
npm start          # http://localhost:4200
```

**Para empezar de cero:** `docker compose down -v` borra la base; al volver a levantar la API se
siembra de nuevo.

### WhatsApp en local

Sin gateway configurado la agenda funciona igual, y en la ficha de cada turno los avisos aparecen
como "No se envió" (la cuenta no tiene WhatsApp vinculado). Para probar el circuito completo hay que
apuntar a un WabixGatewayBE con un número de prueba:

```bash
export WhatsappGateway__BaseUrl="https://<gateway>"
export WhatsappGateway__ApiKey="<api key de la empresa en el gateway>"
export WhatsappGateway__WebhookSecret="<secreto>"
dotnet run --project Web
```

Después, como superadmin: `PUT /api/tenants/1/whatsapp` con `{ "telefonoId": <id del teléfono en el gateway> }`.
En el gateway, ese teléfono lleva `Etiqueta` = `1` (el id de la cuenta) y su webhook apunta a
`<url pública de la API>/api/webhooks/whatsapp` con el mismo secreto. En Configuración → Tu WhatsApp
aparece el QR para vincular el número.

## Comandos

```bash
dotnet build Turnero.sln
dotnet test Tests/Tests.csproj
dotnet ef migrations add <Nombre> --project Infrastructure --startup-project Web
```

## Producción

Turnix corre en el VPS (API + Postgres en Docker, detrás de Caddy) y el front en Vercel. Cada push a
`main` se despliega solo. Pasos de instalación, WhatsApp y operación: [docs/deploy-vps.md](./docs/deploy-vps.md).
