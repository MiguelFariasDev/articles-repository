#!/bin/bash
# Sobe o ambiente local completo: infra (Docker), migration, API e Worker.
# Portas do host podem ser alteradas em .env (DB_PORT, REDIS_PORT, LOCALSTACK_PORT).
set -euo pipefail
cd "$(dirname "$0")/.."

if [ -f .env ]; then set -a; . ./.env; set +a; fi
DB_PORT=${DB_PORT:-5432}
REDIS_PORT=${REDIS_PORT:-6379}
LOCALSTACK_PORT=${LOCALSTACK_PORT:-4566}
LOG_DIR=.run
mkdir -p "$LOG_DIR"

echo "==> docker compose up"
docker compose up -d

echo "==> aguardando healthchecks (postgres, redis, localstack)"
for i in $(seq 1 60); do
  unhealthy=$(docker compose ps --format '{{.Service}} {{.Health}}' | grep -v ' healthy' || true)
  [ -z "$unhealthy" ] && break
  sleep 2
done
docker compose ps

# Overrides de configuração (sobrescrevem os appsettings)
export ConnectionStrings__Rds="Host=localhost;Port=$DB_PORT;Database=${DB_NAME:-artigos};Username=${DB_USER:-postgres};Password=${DB_PASSWORD:-postgres}"
export DB_CONNECTION="$ConnectionStrings__Rds"
export Redis__Connection="localhost:$REDIS_PORT"
export AWS__ServiceUrl="http://localhost:$LOCALSTACK_PORT"
export AWS__SqsQueueUrl="http://localhost:$LOCALSTACK_PORT/000000000000/articles-repository-queue"
export ASPNETCORE_ENVIRONMENT=Development DOTNET_ENVIRONMENT=Development

echo "==> dotnet ef database update"
dotnet ef database update \
  --project src/ArticlesRepository.Infrastructure \
  --startup-project src/ArticlesRepository.Api

echo "==> dotnet build"
dotnet build -v q --nologo

echo "==> subindo API e Worker em background (logs em $LOG_DIR/)"
nohup dotnet run --no-build --project src/ArticlesRepository.Api > "$LOG_DIR/api.log" 2>&1 &
echo $! > "$LOG_DIR/api.pid"
nohup dotnet run --no-build --project src/ArticlesRepository.Worker > "$LOG_DIR/worker.log" 2>&1 &
echo $! > "$LOG_DIR/worker.pid"

echo
echo "Swagger : http://localhost:5019/swagger"
echo "Health  : http://localhost:5019/health"
echo "Parar   : kill \$(cat $LOG_DIR/api.pid $LOG_DIR/worker.pid) && docker compose down"
