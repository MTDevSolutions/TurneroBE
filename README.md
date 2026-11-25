# TurneroBE

Backend del turnero para profesionales de la salud (.NET 10, PostgreSQL, multi-tenant).
Parte de [BaseTenant](https://github.com/MTDevSolutions/BaseTenant) con las mejoras de CellFixBE.

```bash
docker compose up -d
dotnet ef database update --project Infrastructure --startup-project Web
dotnet run --project Web        # http://localhost:53714/swagger
dotnet test Tests/Tests.csproj
```

Ver [CLAUDE.md](./CLAUDE.md) para arquitectura, convenciones y credenciales de desarrollo.
