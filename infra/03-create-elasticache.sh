#!/bin/bash
source "$(dirname "$0")/00-common.sh"

SG_EC2="$(ensure_sg "$SG_EC2_NAME" "Articles Repository EC2")"
SG_REDIS="$(ensure_sg "$SG_REDIS_NAME" "Articles Repository Redis")"
allow_from_sg "$SG_REDIS" 6379 "$SG_EC2"
save_state SG_EC2 "$SG_EC2"; save_state SG_REDIS "$SG_REDIS"

if aws elasticache describe-cache-clusters --cache-cluster-id "$REDIS_ID" >/dev/null 2>&1; then
  log "ElastiCache já existe"
else
  log "Criando ElastiCache Redis $REDIS_ID (cache.t3.micro, single-node)"
  aws elasticache create-cache-cluster \
    --cache-cluster-id "$REDIS_ID" \
    --engine redis \
    --cache-node-type cache.t3.micro \
    --num-cache-nodes 1 \
    --security-group-ids "$SG_REDIS" >/dev/null
fi

log "Aguardando o cluster ficar disponível (3-8 min)..."
aws elasticache wait cache-cluster-available --cache-cluster-id "$REDIS_ID"

ENDPOINT="$(aws elasticache describe-cache-clusters --cache-cluster-id "$REDIS_ID" --show-cache-node-info \
            --query 'CacheClusters[0].CacheNodes[0].Endpoint.Address' --output text)"
save_state REDIS_ENDPOINT "$ENDPOINT"
log "OK: endpoint Redis = $ENDPOINT"
