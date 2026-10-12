#!/bin/bash
# Parte 2 — Application Load Balancer: sg-alb, target group (health check /health) e listener HTTP:80.
source "$(dirname "$0")/00-common.sh"
load_state
: "${SG_EC2:?Rode os scripts 01 a 07 antes (falta SG_EC2 em .state.env)}"

VPC="$(default_vpc)"
read -r -a SUBNETS <<<"$(default_subnets)"
[ "${#SUBNETS[@]}" -ge 2 ] || { echo "O ALB precisa de subnets em pelo menos 2 AZs." >&2; exit 1; }

SG_ALB="$(ensure_sg "$SG_ALB_NAME" "Articles Repository ALB")"
allow_from_cidr "$SG_ALB" 80 "0.0.0.0/0"
save_state SG_ALB "$SG_ALB"

# A API na EC2 passa a aceitar tráfego da porta 5000 apenas vindo do ALB.
allow_from_sg "$SG_EC2" 5000 "$SG_ALB"
aws ec2 revoke-security-group-ingress --group-id "$SG_EC2" --protocol tcp --port 5000 --cidr 0.0.0.0/0 \
  >/dev/null 2>&1 || true

TG_ARN="$(aws elbv2 describe-target-groups --names "$TG_NAME" --query 'TargetGroups[0].TargetGroupArn' --output text 2>/dev/null || echo None)"
if [ "$TG_ARN" = "None" ]; then
  log "Criando target group $TG_NAME (health check GET /health)"
  TG_ARN="$(aws elbv2 create-target-group --name "$TG_NAME" --protocol HTTP --port 5000 --vpc-id "$VPC" \
    --target-type instance \
    --health-check-protocol HTTP --health-check-path /health \
    --health-check-interval-seconds 30 --health-check-timeout-seconds 10 \
    --healthy-threshold-count 2 --unhealthy-threshold-count 3 \
    --matcher HttpCode=200 \
    --query 'TargetGroups[0].TargetGroupArn' --output text)"
  aws elbv2 modify-target-group-attributes --target-group-arn "$TG_ARN" \
    --attributes Key=deregistration_delay.timeout_seconds,Value=30 >/dev/null
else
  log "Target group já existe"
fi
save_state TG_ARN "$TG_ARN"

ALB_ARN="$(aws elbv2 describe-load-balancers --names "$ALB_NAME" --query 'LoadBalancers[0].LoadBalancerArn' --output text 2>/dev/null || echo None)"
if [ "$ALB_ARN" = "None" ]; then
  log "Criando ALB $ALB_NAME (internet-facing, ${#SUBNETS[@]} subnets)"
  ALB_ARN="$(aws elbv2 create-load-balancer --name "$ALB_NAME" --type application --scheme internet-facing \
    --subnets "${SUBNETS[@]}" --security-groups "$SG_ALB" \
    --query 'LoadBalancers[0].LoadBalancerArn' --output text)"
else
  log "ALB já existe"
fi
save_state ALB_ARN "$ALB_ARN"

LISTENER="$(aws elbv2 describe-listeners --load-balancer-arn "$ALB_ARN" --query 'Listeners[?Port==`80`].ListenerArn | [0]' --output text)"
if [ "$LISTENER" = "None" ] || [ -z "$LISTENER" ]; then
  log "Criando listener HTTP:80 → $TG_NAME"
  aws elbv2 create-listener --load-balancer-arn "$ALB_ARN" --protocol HTTP --port 80 \
    --default-actions Type=forward,TargetGroupArn="$TG_ARN" >/dev/null
fi

log "Aguardando o ALB ficar ativo..."
aws elbv2 wait load-balancer-available --load-balancer-arns "$ALB_ARN"
DNS="$(aws elbv2 describe-load-balancers --load-balancer-arns "$ALB_ARN" --query 'LoadBalancers[0].DNSName' --output text)"
save_state ALB_DNS "$DNS"
log "OK: ALB em http://$DNS"
