#!/bin/bash
source "$(dirname "$0")/00-common.sh"

if aws dynamodb describe-table --table-name "$TABLE" >/dev/null 2>&1; then
  log "Tabela $TABLE já existe"
else
  log "Criando tabela $TABLE (PK Id, SK Timestamp, PAY_PER_REQUEST)"
  aws dynamodb create-table \
    --table-name "$TABLE" \
    --attribute-definitions AttributeName=Id,AttributeType=S AttributeName=Timestamp,AttributeType=S \
    --key-schema AttributeName=Id,KeyType=HASH AttributeName=Timestamp,KeyType=RANGE \
    --billing-mode PAY_PER_REQUEST >/dev/null
fi
aws dynamodb wait table-exists --table-name "$TABLE"

log "Habilitando TTL no atributo Ttl"
aws dynamodb update-time-to-live --table-name "$TABLE" \
  --time-to-live-specification "Enabled=true,AttributeName=Ttl" >/dev/null 2>&1 || log "TTL já habilitado"

save_state DYNAMODB_TABLE "$TABLE"
log "OK: $TABLE"
