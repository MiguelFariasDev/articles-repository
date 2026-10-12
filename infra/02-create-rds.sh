#!/bin/bash
source "$(dirname "$0")/00-common.sh"

SG_EC2="$(ensure_sg "$SG_EC2_NAME" "Articles Repository EC2")"
SG_RDS="$(ensure_sg "$SG_RDS_NAME" "Articles Repository RDS")"
allow_from_sg "$SG_RDS" 5432 "$SG_EC2"
save_state SG_EC2 "$SG_EC2"; save_state SG_RDS "$SG_RDS"

load_state
if [ -z "${DB_PASSWORD:-}" ]; then
  DB_PASSWORD="$(head -c 24 /dev/urandom | base64 | tr -dc 'A-Za-z0-9' | head -c 24)"
  save_state DB_PASSWORD "$DB_PASSWORD"
  log "Senha do banco gerada e salva em infra/.state.env"
fi

STATUS="$(aws rds describe-db-instances --db-instance-identifier "$DB_ID" \
          --query 'DBInstances[0].DBInstanceStatus' --output text 2>/dev/null || echo none)"
if [ "$STATUS" = "none" ]; then
  log "Criando RDS PostgreSQL $DB_ID (db.t3.micro, 20GB gp3, single-AZ, backup 7 dias)"
  aws rds create-db-instance \
    --db-instance-identifier "$DB_ID" \
    --db-instance-class db.t3.micro \
    --engine postgres \
    --allocated-storage 20 --storage-type gp3 \
    --master-username "$DB_USER" --master-user-password "$DB_PASSWORD" \
    --db-name "$DB_NAME" \
    --backup-retention-period 7 \
    --no-multi-az --no-publicly-accessible \
    --vpc-security-group-ids "$SG_RDS" >/dev/null
else
  log "RDS já existe (status: $STATUS)"
fi

log "Aguardando o RDS ficar disponível (5-10 min)..."
aws rds wait db-instance-available --db-instance-identifier "$DB_ID"

ENDPOINT="$(aws rds describe-db-instances --db-instance-identifier "$DB_ID" \
            --query 'DBInstances[0].Endpoint.Address' --output text)"
save_state DB_ENDPOINT "$ENDPOINT"
log "OK: endpoint RDS = $ENDPOINT"
