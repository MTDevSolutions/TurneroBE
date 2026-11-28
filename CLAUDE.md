# CLAUDE.md — TurneroBE (.NET + EF Core + PostgreSQL)

Backend del SaaS multi-tenant de turnos para profesionales de la salud independientes
(fonoaudiólogas, psicólogas, médicos generales) en Argentina. **Este repo es solo el backend.**

| Repo | Qué contiene |
|---|---|
| `TurneroBE` (este) | Backend .NET, Clean Architecture 6 capas |
| `TurneroFE` | Frontend Angular 20 + PrimeNG |
| `BaseTenant` | Starter multi-tenant del que parte este repo |
| `WabixGatewayBE` | Gateway de WhatsApp (Baileys) que usa el turnero para avisos y respuestas |

> **Copiar la base de BaseTenant y las mejoras de CellFixBE.** Antes de crear cualquier capa,
> entidad, servicio o controller, mirar el equivalente ahí y replicar el patrón.

---

## 1. Producto (MVP)

- El profesional usa un calendario y una lista de pacientes, y carga los turnos.
- El paciente solo interactúa por WhatsApp: recibe aviso y recordatorio y responde Sí (confirma)
  o No (cancela, se libera el horario y se avisa al profesional en su celular personal).
- Cada profesional tiene un número de WhatsApp propio, vinculado por QR en el gateway.
- Canal no oficial (Baileys): la cuenta acepta términos de uso explícitos antes de operar.
- Fuera del MVP: reservar o reprogramar por chat, historia clínica, API oficial de Meta.

**Dos planos de acceso** (igual que BaseTenant):

| Plano | Quién | Qué hace |
|---|---|---|
| Plataforma | Superadmin (dueño del SaaS) | Crea cuentas, registra cobros, suspende/reactiva |
| Tenant | El profesional (rol `admin`) | Opera su agenda dentro de su cuenta |

---

## 2. Arquitectura (6 capas, Clean Architecture)

```
/
├── Turnero.sln
├── Domain/            ← Entities (solo POCOs, sin dependencias)
├── Application/       ← UseCases, Services, Interfaces, Helpers, Exceptions
├── Infrastructure/    ← Data (DbContext + Repositories), Migrations, Security, Services
├── Shared/            ← Responses, DTOs, Constantes (Cte*)
├── IoC/               ← DependencyInjection.cs (todos los services.AddX())
├── Web/               ← Controllers, Middleware, Helpers, Program.cs, appsettings.json
└── Tests/             ← xUnit (unitarios de servicios, fakes in-memory)
```

**Web referencia solo a IoC**, no a Infrastructure directamente.

## 3. Patrones

- **Response wrapper:** toda respuesta usa `BaseResponse<T>` (`Success`, `Message`, `Data`, `ToastShow`).
- **Errores:** `AppException` (factories estáticas) → `ExceptionHandlingMiddleware`. 400 validación,
  401 auth, 403 suspendida / términos pendientes / sin permiso, 404, 409, 503 DB caída.
- **Auth:** JWT HS256 (`JwtSettings`), BCrypt, token también por cookie `access_token`.
- **CSRF:** double-submit (`X-CSRF-Token` contra la cookie `csrf_token`), solo en requests que mutan
  autenticados por cookie y sin header `Authorization`.
- **Repository + Service + UnitOfWork:** los repos no persisten; el servicio commitea con `IUnitOfWork`.
- **Naming en español.** PascalCase en tablas/columnas. Estados y roles como texto manejado en C#.

## 4. Multi-tenant

- Tenant = la cuenta del profesional. Entidades de plataforma (sin filtro): `Tenant`,
  `PagoSuscripcion`, `Superadmin`.
- Entidades de dominio: implementan `ITenantEntity` → global query filter por `TenantId` y
  autocompletado en los inserts. Hoy: `Sucursal` (= consultorio), `Usuario`, `Configuracion`,
  `Paciente`, `Turno`, `MensajeWhatsapp`, `MensajeEntrante`, `PlantillaMensaje`, `NotaConsulta`,
  `NotaConsultaVersion`.
- JWT de la cuenta lleva `tenant_id` + `rol`; el del superadmin no lleva `tenant_id`
  (`tipo_acceso=plataforma`). Policies `Tenant` y `Superadmin`.
- `TenantSuspendidoMiddleware` → 403 `TENANT_SUSPENDIDO`.
- `TerminosPendientesMiddleware` → 403 `TERMINOS_PENDIENTES` si la cuenta no aceptó
  `CteTerminos.VersionVigente`. Libres: `/api/auth/*` y `/api/terminos/*`. Cambiar el texto de los
  términos = subir la versión (todas las cuentas vuelven a aceptar).

## 5. Agenda

- `Paciente.Celular` normalizado y único por cuenta (`UX_Pacientes_TenantId_Celular`): es la clave
  para reconocer la respuesta Sí/No. Búsqueda sin acentos (`unaccent`).
- `Turno`: Inicio/Fin en UTC, estados `pendiente | confirmado | cancelado | atendido | ausente`
  (`MaquinaEstadosTurno`). Cancelado, atendido y ausente son finales.
- Dos turnos vivos (no cancelados) no se superponen: chequeo en `TurnoService` + exclusion
  constraint `EX_Turnos_SinSolapamiento` (btree_gist, rango `[Inicio, Fin)`), que el UnitOfWork
  traduce a 409. Mover un turno de horario (cambiar el inicio) lo vuelve a `pendiente`; estirarlo no.

- `Configuracion.ColorPrincipal`: paleta del color de la app (`CteColores`: rosa, lavanda, coral, salvia,
  celeste). Se cambia con `PUT /api/configuracion/color` y viaja en el login para pintar desde el arranque.

## 5.1 Notas de consulta (fase A de historia clínica)

- `NotaConsulta`: texto libre del profesional sobre un paciente, atada a un turno `atendido` del mismo
  paciente o suelta (`TurnoId` null). Varias por turno. Solo el rol `admin` (`NotaConsultaService`
  responde 403 a cualquier otro). No sale nunca por WhatsApp; la agenda solo ve `TurnoDto.CantidadNotas`.
- Se edita solo dentro de `CteNotasConsulta.HorasEdicion` (24 h) desde el alta, con `version` para
  detectar ediciones cruzadas (409); cada edición guarda el texto anterior en `NotaConsultaVersion`.
  Después solo se anula con motivo (el texto queda). **Nunca se borra:** no hay endpoint DELETE y la
  migración `NotasConsulta` agrega triggers que rechazan DELETE/TRUNCATE de notas y UPDATE/DELETE de versiones.
- Ficha del paciente: `GET /api/pacientes/{id}/ficha` (resumen + turnos con cantidad de notas). Las
  notas van aparte: `GET|POST /api/pacientes/{id}/notas`, `PUT /api/notas/{id}`,
  `POST /api/notas/{id}/anular`, `GET /api/notas/{id}/versiones`.
- Todavía no es historia clínica formal (Ley 26.529): faltan exportación, firma, auditoría de lecturas
  y adjuntos (fases B y C). No llamarla "historia clínica" en el producto hasta entonces.

## 6. WhatsApp

- Se usa WabixGatewayBE sin cambios. Config en `WhatsappGateway` (`BaseUrl`, `ApiKey`, `WebhookSecret`).
  El teléfono de cada cuenta se vincula desde la plataforma (`PUT /api/tenants/{id}/whatsapp`) y en el
  gateway su `Etiqueta` es el **id del tenant** y su webhook apunta a `POST /api/webhooks/whatsapp`.
- **Salida (outbox):** `AvisoTurnoService` encola en `MensajesWhatsapp` (aviso al agendar, cambio de
  horario, cancelación del profesional). `RecordatoriosWorker` (cada minuto) encola el recordatorio a
  `HorasRecordatorio` del inicio (`Turno.RecordatorioEncoladoEn` evita duplicarlo).
  `EnvioWhatsappWorker` (cada 5 s) reclama con `FOR UPDATE SKIP LOCKED` + lease, reintenta con backoff
  y descarta si la cuenta está suspendida, sin términos vigentes o sin teléfono.
- **Textos editables** (igual que CellFix): los de fábrica viven en `CtePlantillasMensaje` y en
  `PlantillasMensaje` solo está lo que la cuenta reescribió (restablecer = borrar la fila). Variables
  `{paciente}`, `{apellido}`, `{profesional}`, `{fecha}`, `{hora}`, `{lugar}` (`PlantillaMensajeRenderer`;
  una desconocida da 400). Los que piden respuesta llevan un **pie fijo no editable** con el "Respondé SÍ/NO",
  porque de eso depende el flujo. El recordatorio tiene dos textos (sin confirmar / ya confirmado).
  Endpoints `GET /api/whatsapp/plantillas`, `PUT|DELETE /api/whatsapp/plantillas/{clave}` (solo `admin`) y
  `POST /api/whatsapp/plantillas/vista-previa`. Se arman en `TextosWhatsapp.Armar`; lo ya encolado no cambia.
- **Entrada:** el webhook valida `X-Webhook-Secret`, deduplica por id del gateway
  (`UX_MensajesEntrantes_GatewayMensajeId`) y guarda todo en `MensajesEntrantes`. Se aplica al próximo
  turno vivo del paciente (por celular): Sí → confirmado; No → cancelado (`CanceladoPor = paciente`),
  se libera el horario y se avisa a los celulares de los admins. Otro texto no se contesta (`InterpreteRespuesta`).
- **Desconexión:** el gateway avisa los cambios de sesión del número en `POST /api/webhooks/whatsapp/sesion`
  (`AvisoSesionWhatsappService`): se guardan en `Tenant.WhatsappEstadoSesion/EstadoDesde/UltimoAvisoId` (un aviso
  con id menor se ignora; cambiar de teléfono los resetea). `GET /api/whatsapp/alerta` dice si mostrar el cartel
  (lee la base, no al bot). Por decisión del producto no se manda WhatsApp a la profesional: saldría del número caído.
- Webhook y workers corren sin tenant en el request: usan `IgnoreQueryFilters()` con `TenantId` explícito.
- Teléfonos se guardan normalizados para Argentina con `TelefonoWhatsappHelper` (549 + área + número).
- Horarios de los textos en hora de Argentina (`HoraArgentina`).

## 7. Comandos

```bash
docker compose up -d                       # Postgres local en :5436
dotnet build Turnero.sln
dotnet run --project Web                   # http://localhost:5080/swagger
dotnet test Tests/Tests.csproj
dotnet ef migrations add <Nombre> --project Infrastructure --startup-project Web
dotnet ef database update --project Infrastructure --startup-project Web
```

Seed de Development: superadmin `admin@turnero.local` / `Admin1234!`; cuentas demo
`psico@demo.local` (términos aceptados, con 8 pacientes y una semana de turnos de ejemplo) y
`fono@demo.local` (pendientes, sin datos), password `Demo1234!`. Fuera de Development la API no arranca
con la clave JWT ni la password de superadmin de ejemplo.

## 7.1 Producción (Turnix)

El producto se llama **Turnix**. Runbook completo: `docs/deploy-vps.md`.
- API en el VPS (`https://turnix-api.mtdevapp.com`, Caddy → `127.0.0.1:5007`), con `docker-compose.prod.yml`
  (`turnix_backend` + `turnix_db`) unido a la red `wabix-gateway_default` para hablar con el gateway.
- Push a `main` → `.github/workflows/deploy.yml`: tests, imagen en GHCR y `scripts/deploy.sh` por SSH
  (backup previo, healthcheck en `GET /`, rollback a la imagen anterior si no responde).
- Front en Vercel (`https://turnix.mtdevapp.com`): reenvía `/api/*` a la API, así que cookies `Lax` y CSRF activo.
- En producción no hay cuentas demo: las crea el superadmin (`POST /api/tenants`).

## 8. Ramas

`main` = producción, `dev` = test. Las features salen siempre desde `dev` y el PR va contra `dev`.

## 9. Reglas de interacción con Claude

1. Antes de escribir/modificar código: explicar qué se va a hacer y por qué, y esperar
   confirmación explícita ("dale").
2. Revisar siempre la migración generada antes de aplicarla (que un rename no salga como drop + create).
3. Tests siempre en `Tests/` (xUnit), unitarios de servicios con fakes in-memory (sin BD).
4. Si hay ambigüedad sobre un endpoint o el contrato request/response, preguntar antes de asumir.
