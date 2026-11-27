# Deploy de Turnix (VPS Hetzner + Vercel)

Turnix = este backend (TurneroBE) + el front (TurneroFE). Mismo esquema que CellFix
(`CellFixBE/docs/guia-vps-hetzner.md`): imagen en GHCR, deploy por SSH en cada push a `main`, Caddy con TLS.

| Pieza | Dónde | Detalle |
|---|---|---|
| Front | Vercel | `https://turnix.mtdevapp.com`, rama `main` de TurneroFE |
| API | VPS `188.245.44.207` | `https://turnix-api.mtdevapp.com` → Caddy → `127.0.0.1:5007` |
| Contenedores | VPS, `~/turnix` | `turnix_backend` (5007), `turnix_db` (Postgres 16, `127.0.0.1:5437`) |
| WhatsApp | VPS, `~/wabix-gateway` | bot `whatsapp-bot-4`, empresa "Turnix", modo webhook por la red docker |

El front **no llama a la API por otro dominio**: `vercel.json` reenvía `/api/*` a `turnix-api`. Para el
navegador todo es `turnix.mtdevapp.com`, así que las cookies quedan `Lax` y el CSRF funciona tal cual.
La API pública solo la usan Vercel y, si se habilita, Swagger.

Los comandos de abajo asumen el usuario `mtdev` en el VPS. Los valores entre `<...>` se reemplazan.

---

## 1. DNS en Cloudflare (`mtdevapp.com`)

| Tipo | Nombre | Destino | Proxy |
|---|---|---|---|
| A | `turnix-api` | `188.245.44.207` | **DNS only** (nube gris: Let's Encrypt tiene que validar) |
| CNAME | `turnix` | lo que indique Vercel al agregar el dominio (paso 9), normalmente `cname.vercel-dns.com` | **DNS only** |

```bash
nslookup turnix-api.mtdevapp.com 8.8.8.8     # debe dar 188.245.44.207
```

## 2. Revisar el VPS antes de sumar algo

```bash
free -h                         # memoria libre (Turnix ~200 MB + Postgres ~100 MB + bot ~200 MB)
docker stats --no-stream        # consumo de lo que ya corre
ss -ltnp | grep -E ':(5007|5437)\b' || echo "puertos libres"
docker network ls --filter name=wabix   # tiene que existir wabix-gateway_default
```

Puertos ya usados en el VPS: 5001–5006 (gymtracker, agrotrack, cellfix, dixit, appgastos, gateway), 5050 y 5433–5436.
Si `5007` o `5437` están ocupados, cambiarlos en `docker-compose.prod.yml` y en `scripts/deploy.sh`
(`HEALTH_URL`) antes de seguir.

## 3. Clonar el repo (deploy key de solo lectura)

```bash
ssh-keygen -t ed25519 -f ~/.ssh/turnix_deploy -N "" -C "turnix-deploy-vps"
cat ~/.ssh/turnix_deploy.pub
```

En GitHub: **TurneroBE → Settings → Deploy keys → Add deploy key**, pegar la pública, **sin** "Allow write access".

```bash
cd ~
GIT_SSH_COMMAND="ssh -i ~/.ssh/turnix_deploy -o IdentitiesOnly=yes" \
  git clone -b main git@github.com:MTDevSolutions/TurneroBE.git turnix
ls -l ~/turnix/scripts/deploy.sh     # tiene que tener permiso x
```

## 4. Crear el `.env`

```bash
cd ~/turnix
cp .env.example .env
chmod 600 .env
openssl rand -base64 32   # POSTGRES_PASSWORD
openssl rand -base64 48   # JWT_KEY
openssl rand -hex 32      # WHATSAPP_WEBHOOK_SECRET (también va al gateway como BOT_4_WEBHOOK_SECRET)
nano .env
```

Completar todo lo que dice `CAMBIAR`. La API **no arranca** con la clave JWT ni la password de superadmin
de desarrollo. `WHATSAPP_GATEWAY_API_KEY` se completa en el paso 10; hasta entonces WhatsApp queda apagado
y el resto funciona igual.

Para el alta de la primera cuenta (paso 8) conviene `ENABLE_SWAGGER=true` con `SWAGGER_PASSWORD` fuerte;
después se puede volver a `false`.

## 5. Caddy

```bash
sudo nano /etc/caddy/Caddyfile
```

```
turnix-api.mtdevapp.com {
    reverse_proxy localhost:5007
}
```

```bash
cat /etc/caddy/Caddyfile                       # confirmar que no se cortó nada al pegar
sudo caddy validate --config /etc/caddy/Caddyfile
sudo systemctl reload caddy
```

## 6. Deploy automático (setup único)

**6.1 Clave SSH exclusiva del CI** (en tu PC):

```bash
ssh-keygen -t ed25519 -f ~/.ssh/turnix_ci -N "" -C "turnix-ci"
cat ~/.ssh/turnix_ci.pub
```

En el VPS, **una línea** más en `~/.ssh/authorized_keys` (la clave solo puede correr el script):

```
restrict,command="/home/mtdev/turnix/scripts/deploy.sh" ssh-ed25519 AAAA...(la pública) turnix-ci
```

**6.2 Secrets en GitHub** (TurneroBE → Settings → Secrets and variables → Actions):

| Secret | Valor |
|---|---|
| `VPS_HOST` | `188.245.44.207` |
| `VPS_USER` | `mtdev` |
| `VPS_SSH_KEY` | contenido **completo** de la privada `~/.ssh/turnix_ci` |
| `VPS_KNOWN_HOSTS` | salida de `ssh-keyscan -t ed25519 188.245.44.207` |

**6.3 Login a GHCR en el VPS.** Si ya está hecho para CellFix (`docker login ghcr.io`) y el token puede leer
los paquetes de la organización, no hace falta repetirlo.

## 7. Primer deploy

Pasar `dev` a `main` (merge del PR `dev → main`). El workflow **Deploy** corre los tests, sube la imagen
`ghcr.io/mtdevsolutions/turnerobe:<sha>` y ejecuta `scripts/deploy.sh` en el VPS: crea la base, aplica las
migraciones y siembra el superadmin.

```bash
curl -s https://turnix-api.mtdevapp.com/ ; echo   # {"success":true,...} o redirige a /swagger
docker logs --tail 50 turnix_backend
```

Si falló: `docker logs turnix_backend` casi siempre muestra el problema (una variable del `.env`).
Para reintentar a mano: `~/turnix/scripts/deploy.sh <sha-de-40-caracteres>`.

## 8. Primera cuenta (superadmin)

En producción **no hay cuentas demo**. Con Swagger (`https://turnix-api.mtdevapp.com/swagger`, basic auth)
o con curl desde el VPS:

```bash
API=https://turnix-api.mtdevapp.com/api
TOKEN=$(curl -s $API/auth/superadmin/login -H 'Content-Type: application/json' \
  -d '{"email":"<SUPERADMIN_EMAIL>","password":"<SUPERADMIN_PASSWORD>"}' | jq -r .data.token)

curl -s $API/tenants -H "Authorization: Bearer $TOKEN" -H 'Content-Type: application/json' -d '{
  "nombre": "Cuenta de prueba",
  "especialidad": "psicologia",
  "consultorioNombre": "Consultorio",
  "adminNombre": "<tu nombre>",
  "adminEmail": "<tu email>",
  "adminPassword": "<password de 8+ caracteres>",
  "adminCelular": "<tu celular, ej. 11 2345-6789>"
}' | jq .
```

Anotar el `id` de la cuenta: es la `etiqueta` del teléfono en el gateway (paso 10).
El `adminCelular` es donde llega el aviso cuando un paciente cancela por WhatsApp.

## 9. Front en Vercel

1. **Add New → Project →** importar `MTDevSolutions/TurneroFE`. Vercel toma la configuración de `vercel.json`
   (Angular, `npm run build`, salida `dist/turnero/browser`).
2. **Settings → Git → Production Branch:** `main`.
3. **Settings → Domains:** agregar `turnix.mtdevapp.com` y crear en Cloudflare el registro que indique (paso 1).
4. Si se prueba primero con el `*.vercel.app`, sumarlo a `CORS_ALLOWED_ORIGIN` del `.env` (separado por coma)
   y recrear el backend: `cd ~/turnix && docker compose -f docker-compose.prod.yml up -d backend`.

Probar: entrar a `https://turnix.mtdevapp.com` con la cuenta del paso 8 y aceptar los términos.

## 10. WhatsApp (bot 4 en el gateway)

Estado actual (2026-11-27): empresa **Turnix = id 3**, teléfono **id 5** (`+5493463586417`, número de prueba),
bot `whatsapp-bot-4`, etiqueta `1` (la cuenta de prueba), modo webhook. Probado de punta a punta: aviso, Sí y No.

Requiere el bot 4 en `main` de WabixGatewayBE y **un número que no sea el personal de nadie** (Baileys no es un
canal oficial: WhatsApp puede bloquearlo).

> **Trampa del `source .env`.** Para tener `$ADMIN_TOKEN` se carga el `.env` del gateway en la terminal. Desde ese
> momento Docker Compose usa esas variables **por encima** de cualquier `.env`: al levantar Turnix toma el
> `POSTGRES_PASSWORD` del gateway, y si `BOT_4_TELEFONO_ID` estaba vacío al cargarlo, el bot queda entregando a
> `.../baileys/` sin id (404 en todos los entrantes). **Todo `docker compose up` de este paso va con
> `env -i PATH="$PATH" HOME="$HOME"`**, igual que `scripts/deploy.sh`. Al terminar, cerrar esa sesión SSH.

### 10.1 Traer el gateway y preparar variables

En el VPS, `github.com` está mapeado en `~/.ssh/config` a la clave de **agrotrack**: el pull del gateway va con
su clave explícita.

```bash
cd ~/wabix-gateway
GIT_SSH_COMMAND="ssh -i ~/.ssh/wabix_deploy -o IdentitiesOnly=yes" git pull --ff-only
set -a; source .env; set +a      # $ADMIN_TOKEN (y BOT_4_* si ya existen)
GW=https://wabix-gateway.mtdevapp.com/api
TENANT_ID=$(docker exec turnix_db psql -U postgres -d turnix -Atc 'select min("Id") from "Tenants"')
WEBHOOK_SECRET=$(grep -E '^WHATSAPP_WEBHOOK_SECRET=' ~/turnix/.env | cut -d= -f2-)
BOT_4_API_KEY=${BOT_4_API_KEY:-$(openssl rand -hex 32)}
BOT_4_SECRETO_ENTRANTE=${BOT_4_SECRETO_ENTRANTE:-$(openssl rand -hex 32)}
set_env() { if grep -q "^$2=" "$1"; then sed -i "s|^$2=.*|$2=$3|" "$1"; else echo "$2=$3" >> "$1"; fi; }
```

### 10.2 Empresa, teléfono y API key

```bash
# Empresa (una sola vez; Turnix ya existe con id 3)
curl -s -X POST $GW/admin/empresas -H "X-Admin-Token: $ADMIN_TOKEN" \
  -H "Content-Type: application/json" -d '{"nombre":"Turnix"}'; echo

# Teléfono en modo webhook. Los secretos van en el body y el gateway nunca los devuelve.
TEL_JSON=$(printf '{"empresaId":3,"numero":"+549<numero>","proveedor":"baileys","modoEntrega":"webhook","etiqueta":"%s","baileysBaseUrl":"http://whatsapp-bot-4:3000","baileysApiKey":"%s","secretoEntrante":"%s","webhookUrl":"http://turnix_backend:5007/api/webhooks/whatsapp","webhookSecret":"%s"}' \
  "$TENANT_ID" "$BOT_4_API_KEY" "$BOT_4_SECRETO_ENTRANTE" "$WEBHOOK_SECRET")
RESP_TEL=$(curl -s -X POST $GW/admin/telefonos -H "X-Admin-Token: $ADMIN_TOKEN" -H "Content-Type: application/json" -d "$TEL_JSON")
echo "$RESP_TEL"                 # tiene que decir "success":true; si no, ver 10.5
TEL_ID=$(echo "$RESP_TEL" | grep -o '"id":[0-9]*' | head -1 | cut -d: -f2)

# Claves del bot en el .env del gateway (BOT_4_WEBHOOK_SECRET = WHATSAPP_WEBHOOK_SECRET de Turnix)
[ -n "$TEL_ID" ] && set_env .env BOT_4_TELEFONO_ID "$TEL_ID"
set_env .env BOT_4_API_KEY "$BOT_4_API_KEY"
set_env .env BOT_4_SECRETO_ENTRANTE "$BOT_4_SECRETO_ENTRANTE"
set_env .env BOT_4_WEBHOOK_SECRET "$WEBHOOK_SECRET"
grep '^BOT_4_TELEFONO_ID=' .env  # tiene que tener número

# API key de la empresa: solo aparece en esta respuesta; va directo al .env de Turnix sin mostrarse
API_KEY=$(curl -s -X POST $GW/admin/empresas/3/apikey -H "X-Admin-Token: $ADMIN_TOKEN" | grep -o '"apiKey":"[^"]*"' | cut -d'"' -f4)
set_env ~/turnix/.env WHATSAPP_GATEWAY_API_KEY "$API_KEY"
```

### 10.3 Levantar el bot y recrear Turnix (con `env -i`)

```bash
cd ~/wabix-gateway
env -i PATH="$PATH" HOME="$HOME" docker compose -f docker-compose.prod.yml up -d --build whatsapp-bot-4
docker inspect wabix-gateway-whatsapp-bot-4-1 --format '{{range .Config.Env}}{{println .}}{{end}}' | grep API_WEBHOOK_URL
# tiene que terminar en /baileys/<BOT_4_TELEFONO_ID>

cd ~/turnix
env -i PATH="$PATH" HOME="$HOME" docker compose -f docker-compose.prod.yml up -d backend
docker logs turnix_backend 2>&1 | grep -i "sin configurar"   # no tiene que aparecer nada
```

### 10.4 Vincular a la cuenta y escanear el QR

Como superadmin (Swagger o curl con el `$TOKEN` del paso 8): `PUT /api/tenants/<id de la cuenta>/whatsapp` con
`{"telefonoId": <BOT_4_TELEFONO_ID>}`. Después, con el celular del número: en Turnix, **Configuración → Tu
WhatsApp**, escanear el QR. El celular no tiene que tener vinculado otro bot (ver 10.5).

### 10.5 Si el número ya está dado de alta en otra empresa

El alta responde `El número ... ya está dado de alta`. Un número es único en todo el gateway (también los
inactivos), no se puede mover de empresa ni borrar por API. Así se pasó el número de prueba de CellFix
(teléfono 3, empresa 2, bot 2) a Turnix:

```bash
# 1. Encontrarlo (no muestra secretos)
for e in 1 2 3 4 5 6; do r=$(curl -s $GW/admin/empresas/$e/telefonos -H "X-Admin-Token: $ADMIN_TOKEN"); \
  echo "$r" | grep -q '<numero sin +>' && echo "empresa $e: $r"; done

# 2. Desactivarlo con la API (PUT con el mismo numero, proveedor, modo, etiqueta y baseUrl que tenía)
curl -s -X PUT $GW/admin/telefonos/<id> -H "X-Admin-Token: $ADMIN_TOKEN" -H "Content-Type: application/json" \
  -d '{"numero":"+<numero>","activo":false,"proveedor":"baileys","modoEntrega":"cola","etiqueta":"<etiqueta>","baileysBaseUrl":"http://whatsapp-bot-<n>:3000"}'

# 3. En el celular: WhatsApp → Dispositivos vinculados → cerrar la sesión del bot viejo. En el VPS, frenarlo:
env -i PATH="$PATH" HOME="$HOME" docker compose -f docker-compose.prod.yml stop whatsapp-bot-<n>

# 4. Liberar el número sin borrar el historial (backup antes; numero es varchar(20))
docker exec wabix-gateway-postgres-1 pg_dump -U wabix -d wabix_gateway -t telefonos | gzip > ~/backups/wabix-telefonos-$(date +%F-%H%M).sql.gz
docker exec wabix-gateway-postgres-1 psql -U wabix -d wabix_gateway -c \
  "UPDATE telefonos SET numero = 'baja-<numero sin +>' WHERE id = <id> AND numero = '<numero sin +>' AND activo = false;"
# tiene que decir UPDATE 1; después repetir el alta de 10.2
```

El producto que usaba ese teléfono (CellFix, por id) deja de mandar desde ese número. Si el número también estaba
vinculado a otro sistema fuera del gateway (pasó con el bot de Bille, `~/appgastos`, servicio `whatsapp-bot`),
frenarlo y cerrar su sesión en el celular: si no, ve y contesta las respuestas de los pacientes.

### Prueba de punta a punta

1. Cargar un paciente con **otro** celular (el tuyo) y darle un turno: llega el aviso.
2. Responder **Sí** (también sirven "si", "1", 👍): el turno pasa a confirmado en la agenda.
3. Dar otro turno y responder **No** ("2", 👎): se cancela, se libera el horario y llega el aviso al `adminCelular`.
4. Un turno para dentro de menos horas que el recordatorio configurado: llega el recordatorio.

### Si las respuestas no llegan

El texto casi nunca es el problema (el intérprete acepta Sí/No con o sin tilde, 1/2 y emojis). Ver dónde se corta
**celular → bot 4 → gateway → Turnix**:

```bash
docker logs wabix-gateway-whatsapp-bot-4-1 --since 1h 2>&1 | grep -i webhook | tail -5
docker exec wabix-gateway-postgres-1 psql -U wabix -d wabix_gateway -c \
  "select id, left(texto,20), entregado_en, intentos, left(ultimo_error,80) from mensajes_entrantes where telefono_id = <id> order by id desc limit 5;"
docker exec turnix_db psql -U postgres -d turnix -c \
  'select "Id", left("Texto",20), "Resultado", "TurnoId" from "MensajesEntrantes" order by "Id" desc limit 5;'
```

| Síntoma | Causa |
|---|---|
| Bot: `el webhook de la API respondió con error`, `status 404`; nada en el gateway | El bot se levantó con `BOT_4_TELEFONO_ID` vacío (trampa del `source .env`). Recrearlo con `env -i ... up -d --force-recreate whatsapp-bot-4` (la sesión se conserva). |
| Gateway: `entregado_en` vacío con error 401 | `webhookSecret` del teléfono ≠ `WHATSAPP_WEBHOOK_SECRET` de Turnix. |
| Gateway: error de conexión | `turnix_backend` no está en la red `wabix-gateway_default` o `webhookUrl` mal escrita. |
| Turnix guarda el mensaje pero no toca el turno | Ver `Resultado`: el número no coincide con ningún paciente o el paciente no tiene un turno vivo próximo. |

## 11. Backups

El backup diario cifrado del VPS (`CellFixBE/scripts/backup-db.sh`, cron) detecta solo todos los contenedores
Postgres: `turnix_db` queda incluido sin tocar nada. Al día siguiente del primer deploy, confirmar que aparece:

```bash
grep turnix_db ~/backups/backup.log | tail -3
```

Además, cada deploy deja un dump previo en `~/backups/turnix-pre-deploy-*.sql.gz` (los últimos 14).

## Operación

```bash
cd ~/turnix
docker compose -f docker-compose.prod.yml ps
docker logs -f --tail 100 turnix_backend
scripts/deploy.sh <sha>                     # redeploy manual de un commit ya publicado en GHCR
```

**Rollback:** el deploy vuelve solo a la imagen anterior si la nueva no responde. Las migraciones **no** se
revierten: para eso está el dump `turnix-pre-deploy-*` de ese deploy.
