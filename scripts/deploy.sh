#!/usr/bin/env bash
# Deploy de Turnix en el VPS. Lo ejecuta el workflow deploy.yml por SSH (la clave del CI esta
# restringida a este script) y tambien se puede correr a mano:  scripts/deploy.sh <sha-de-40-hex>
# Adaptado de scripts/deploy.sh de CellFixBE.
#
# Orden: validar tag -> actualizar el repo (compose/scripts) -> backup de la BD -> bajar la imagen
#        -> levantar -> healthcheck -> si falla, volver a la imagen anterior.
#
# Importante: las migraciones EF se aplican solas al arrancar (RUN_MIGRATIONS=true). Volver a la imagen
# anterior NO revierte la base; el backup previo queda en ~/backups para eso.
set -euo pipefail

IMAGE="ghcr.io/mtdevsolutions/turnerobe"   # debe coincidir con 'image:' de docker-compose.prod.yml
CONTAINER_BACKEND="turnix_backend"
CONTAINER_DB="turnix_db"
HEALTH_URL="http://127.0.0.1:5004/"        # GET / sirve de healthcheck (puerto 5004 del compose)
DEPLOY_KEY="$HOME/.ssh/turnix_deploy"      # deploy key de solo lectura del repo (privado)
BACKUPS_DIR="$HOME/backups"
BACKUPS_A_CONSERVAR=14
ESPERA_SEGUNDOS=90
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

COMPOSE_TAG=""

# Compose en un entorno LIMPIO: las variables exportadas en el shell pisan al .env (a CellFix ya le
# rompio un deploy). Solo se pasa el tag de la imagen.
compose() {
  env -i PATH="$PATH" HOME="$HOME" TURNIX_TAG="$COMPOSE_TAG" \
    docker compose -f "$ROOT/docker-compose.prod.yml" --project-directory "$ROOT" "$@"
}

codigo_http() {
  curl -s -o /dev/null -w '%{http_code}' --max-time 3 "$HEALTH_URL" || true
}

# Sano = responde 2xx/3xx y sigue respondiendo unos segundos despues (descarta un crash-loop que contesta una vez).
esperar_salud() {
  local limite=$((SECONDS + ESPERA_SEGUNDOS)) code
  while (( SECONDS < limite )); do
    code="$(codigo_http)"
    if [[ "$code" =~ ^[23] ]]; then
      sleep 5
      code="$(codigo_http)"
      if [[ "$code" =~ ^[23] ]]; then return 0; fi
    fi
    sleep 2
  done
  return 1
}

leer_env() {  # leer_env CLAVE -> valor del .env, sin cargarlo en el shell
  grep -E "^$1=" "$ROOT/.env" | head -n1 | cut -d= -f2-
}

hacer_backup() {
  # Primer deploy: todavia no hay base que respaldar.
  if ! docker inspect "$CONTAINER_DB" >/dev/null 2>&1; then
    echo "Sin contenedor $CONTAINER_DB todavia (primer deploy): no hay backup que hacer."
    return 0
  fi
  local pg_user pg_db destino
  pg_user="$(leer_env POSTGRES_USER)"
  pg_db="$(leer_env POSTGRES_DB)"
  mkdir -p "$BACKUPS_DIR"
  destino="$BACKUPS_DIR/turnix-pre-deploy-$(date +%F-%H%M)-${1:0:7}.sql.gz"
  docker exec "$CONTAINER_DB" pg_dump -U "$pg_user" "$pg_db" | gzip > "$destino"
  [[ -s "$destino" ]] || { echo "El backup quedo vacio: $destino"; rm -f "$destino"; return 1; }
  echo "Backup: $destino ($(du -h "$destino" | cut -f1))"
  # Rotacion: se conservan los ultimos N.
  ls -1t "$BACKUPS_DIR"/turnix-pre-deploy-*.sql.gz | tail -n +"$((BACKUPS_A_CONSERVAR + 1))" | xargs -r rm --
  BACKUP_ACTUAL="$destino"
}

revertir() {
  echo "==> Deploy fallido. Volviendo a la imagen anterior"
  docker logs --tail 40 "$CONTAINER_BACKEND" 2>&1 || true
  if ! docker image inspect "$IMAGE:previous" >/dev/null 2>&1; then
    echo "No hay imagen anterior (primer deploy): revisar los logs de arriba y el .env."
    return
  fi
  COMPOSE_TAG="previous"
  if compose up -d --no-build backend && esperar_salud; then
    echo "Rollback OK: el backend volvio a la version anterior."
    echo "OJO: si la version nueva aplico migraciones, la base NO se revirtio. Backup previo: ${BACKUP_ACTUAL:-<sin backup>}"
  else
    echo "ROLLBACK FALLO: el backend sigue caido. Revisar a mano en el VPS."
  fi
}

limpiar_imagenes() {
  # Se conservan las 5 versiones mas nuevas de la imagen (la en uso no se puede borrar y se ignora).
  docker images "$IMAGE" --format '{{.Tag}}' | grep -E '^[0-9a-f]{40}$' | tail -n +6 \
    | xargs -r -I{} docker rmi "$IMAGE:{}" >/dev/null 2>&1 || true
  docker image prune -f >/dev/null
}

main() {
  local tag="${1:-${SSH_ORIGINAL_COMMAND:-}}"
  [[ "$tag" =~ ^[0-9a-f]{40}$ ]] || { echo "Tag invalido (se espera el sha de 40 hex): '$tag'"; exit 2; }

  # Un deploy a la vez (por si se lanza uno a mano mientras corre el del CI).
  exec 9>/tmp/turnix-deploy.lock
  flock 9

  echo "==> Deploy de $tag"

  # Trae compose y scripts de main. Repo privado: usa la deploy key de solo lectura.
  echo "==> Actualizando el repo"
  GIT_SSH_COMMAND="ssh -i $DEPLOY_KEY -o IdentitiesOnly=yes" git -C "$ROOT" pull --ff-only

  # Guarda la imagen que corre hoy como ':previous' para poder volver atras.
  local imagen_actual
  imagen_actual="$(docker inspect "$CONTAINER_BACKEND" --format '{{.Image}}' 2>/dev/null || true)"
  if [[ -n "$imagen_actual" ]]; then docker tag "$imagen_actual" "$IMAGE:previous"; fi

  echo "==> Backup de la base"
  hacer_backup "$tag"

  # Si la imagen no se puede bajar, se aborta ANTES de tocar nada.
  echo "==> Bajando la imagen"
  COMPOSE_TAG="$tag"
  compose pull backend

  echo "==> Levantando"
  if ! compose up -d --no-build; then revertir; exit 1; fi

  echo "==> Esperando que responda ($HEALTH_URL)"
  if ! esperar_salud; then revertir; exit 1; fi

  limpiar_imagenes
  echo "==> Deploy OK: $tag"
}

main "$@"
exit $?
