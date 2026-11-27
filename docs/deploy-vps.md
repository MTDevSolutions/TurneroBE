# Deploy de Turnix (VPS Hetzner + Vercel)

Turnix = este backend (TurneroBE) + el front (TurneroFE). Mismo esquema que CellFix
(`CellFixBE/docs/guia-vps-hetzner.md`): imagen en GHCR, deploy por SSH en cada push a `main`, Caddy con TLS.

| Pieza | Dónde | Detalle |
|---|---|---|
| Front | Vercel | `https://turnix.mtdevapp.com`, rama `main` de TurneroFE |
| API | VPS `188.245.44.207` | `https://turnix-api.mtdevapp.com` → Caddy → `127.0.0.1:5004` |
| Contenedores | VPS, `~/turnix` | `turnix_backend` (5004), `turnix_db` (Postgres 16, `127.0.0.1:5437`) |
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
ss -ltnp | grep -E ':(5004|5437)\b' || echo "puertos libres"
docker network ls --filter name=wabix   # tiene que existir wabix-gateway_default
```

Si `5004` o `5437` están ocupados, cambiarlos en `docker-compose.prod.yml` y en `scripts/deploy.sh`
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
    reverse_proxy localhost:5004
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

Requiere el PR `deploy/bot-4-turnix` de WabixGatewayBE mergeado en `main` y **un número que no sea el
personal de nadie** (Baileys no es un canal oficial: WhatsApp puede bloquearlo).

```bash
cd ~/wabix-gateway
GIT_SSH_COMMAND="ssh -i ~/.ssh/<deploy key del gateway>" git pull
set -a; source .env; set +a      # para tener $ADMIN_TOKEN
openssl rand -hex 32             # BOT_4_API_KEY
openssl rand -hex 32             # BOT_4_SECRETO_ENTRANTE
GW=https://wabix-gateway.mtdevapp.com/api

# Empresa
curl -s -X POST $GW/admin/empresas -H "X-Admin-Token: $ADMIN_TOKEN" \
  -H "Content-Type: application/json" -d '{"nombre":"Turnix"}'

# Teléfono en modo webhook (el id que devuelve es BOT_4_TELEFONO_ID)
curl -s -X POST $GW/admin/telefonos -H "X-Admin-Token: $ADMIN_TOKEN" -H "Content-Type: application/json" -d '{
  "empresaId": <id de la empresa>,
  "numero": "+549<numero del bot>",
  "proveedor": "baileys",
  "modoEntrega": "webhook",
  "etiqueta": "<id de la cuenta de Turnix del paso 8>",
  "baileysBaseUrl": "http://whatsapp-bot-4:3000",
  "baileysApiKey": "<BOT_4_API_KEY>",
  "secretoEntrante": "<BOT_4_SECRETO_ENTRANTE>",
  "webhookUrl": "http://turnix_backend:5004/api/webhooks/whatsapp",
  "webhookSecret": "<WHATSAPP_WEBHOOK_SECRET del .env de Turnix>"
}'

# API key de la empresa: solo aparece en esta respuesta
curl -s -X POST $GW/admin/empresas/<id de la empresa>/apikey -H "X-Admin-Token: $ADMIN_TOKEN"
```

Cargar `BOT_4_*` en `~/wabix-gateway/.env` y levantar **solo** el bot nuevo:

```bash
docker compose -f docker-compose.prod.yml up -d --build whatsapp-bot-4
docker compose -f docker-compose.prod.yml logs --tail 30 whatsapp-bot-4
```

En Turnix: `WHATSAPP_GATEWAY_API_KEY=<la API key>` en `~/turnix/.env` y recrear el backend:

```bash
cd ~/turnix && docker compose -f docker-compose.prod.yml up -d backend
```

Vincular el teléfono a la cuenta (superadmin, con el `$TOKEN` del paso 8):

```bash
curl -s -X PUT $API/tenants/<id de la cuenta>/whatsapp -H "Authorization: Bearer $TOKEN" \
  -H 'Content-Type: application/json' -d '{"telefonoId": <BOT_4_TELEFONO_ID>}'
```

Por último, desde el celular del bot: en Turnix, **Configuración → Tu WhatsApp**, escanear el QR.

### Prueba de punta a punta

1. Cargar un paciente con **otro** celular (el tuyo) y darle un turno: llega el aviso.
2. Responder **Sí**: el turno pasa a confirmado en la agenda.
3. Dar otro turno y responder **No**: se cancela, se libera el horario y llega el aviso al `adminCelular`.
4. Un turno para dentro de menos horas que el recordatorio configurado: llega el recordatorio.

Si las respuestas no llegan: `docker logs turnix_backend | grep -i webhook` (401 = secreto distinto entre
`WHATSAPP_WEBHOOK_SECRET` y el `webhookSecret` del teléfono; sin requests = revisar `webhookUrl` y que
`turnix_backend` esté en la red `wabix-gateway_default`).

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
