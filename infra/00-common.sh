#!/bin/bash
# Variáveis e funções compartilhadas pelos scripts de infra (use: source ./00-common.sh).
# Requer AWS CLI v2 configurada (aws configure) com permissões administrativas.
set -euo pipefail
export AWS_PAGER=""

REGION="${REGION:-us-east-1}"
PROJECT="${PROJECT:-articles-repository}"
export AWS_DEFAULT_REGION="$REGION"

ACCOUNT_ID="$(aws sts get-caller-identity --query Account --output text)"
BUCKET="${PROJECT}-pdfs-${ACCOUNT_ID}"
TABLE="${PROJECT}-logs"
TOPIC_NAME="${PROJECT}-topic"
QUEUE_NAME="${PROJECT}-queue"
DLQ_NAME="${PROJECT}-queue-dlq"
DB_ID="${PROJECT}-db"
DB_NAME="artigos"
DB_USER="postgres"
REDIS_ID="${PROJECT}-redis"
ROLE_NAME="${PROJECT}-ec2-role"
PROFILE_NAME="${PROJECT}-ec2-profile"
SG_EC2_NAME="sg-ec2"
SG_RDS_NAME="sg-rds"
SG_REDIS_NAME="sg-redis"
SG_ALB_NAME="sg-alb"
ALB_NAME="${PROJECT}-alb"
TG_NAME="${PROJECT}-tg"
LT_NAME="${PROJECT}-lt"
ASG_NAME="${PROJECT}-asg"

STATE_FILE="$(dirname "${BASH_SOURCE[0]}")/.state.env"   # ignorado pelo Git (contém segredos)
touch "$STATE_FILE"; chmod 600 "$STATE_FILE"

log() { echo "==> $*"; }

# save_state KEY VALUE — grava/atualiza uma chave no .state.env
save_state() {
  grep -v "^$1=" "$STATE_FILE" > "$STATE_FILE.tmp" || true
  echo "$1=$2" >> "$STATE_FILE.tmp"
  mv "$STATE_FILE.tmp" "$STATE_FILE"; chmod 600 "$STATE_FILE"
}
# shellcheck disable=SC1090
load_state() { set -a; . "$STATE_FILE"; set +a; }

default_vpc() {
  aws ec2 describe-vpcs --filters Name=isDefault,Values=true --query 'Vpcs[0].VpcId' --output text
}

# ensure_sg NAME DESCRIPTION → imprime o GroupId (cria se não existir)
ensure_sg() {
  local name="$1" desc="$2" vpc id
  vpc="$(default_vpc)"
  id="$(aws ec2 describe-security-groups --filters Name=group-name,Values="$name" Name=vpc-id,Values="$vpc" \
        --query 'SecurityGroups[0].GroupId' --output text)"
  if [ "$id" = "None" ]; then
    log "Criando security group $name" >&2
    id="$(aws ec2 create-security-group --group-name "$name" --description "$desc" --vpc-id "$vpc" \
          --query GroupId --output text)"
  fi
  echo "$id"
}

# allow_from_sg TARGET_SG PORT SOURCE_SG — idempotente
allow_from_sg() {
  aws ec2 authorize-security-group-ingress --group-id "$1" --protocol tcp --port "$2" --source-group "$3" \
    >/dev/null 2>&1 || true
}
# allow_from_cidr TARGET_SG PORT CIDR — idempotente
allow_from_cidr() {
  aws ec2 authorize-security-group-ingress --group-id "$1" --protocol tcp --port "$2" --cidr "$3" \
    >/dev/null 2>&1 || true
}

# default_subnets → IDs das subnets da VPC padrão, uma por AZ (separados por espaço)
default_subnets() {
  aws ec2 describe-subnets --filters Name=vpc-id,Values="$(default_vpc)" Name=default-for-az,Values=true \
    --query 'Subnets[].SubnetId' --output text
}

# render_user_data DEST — gera o user-data a partir do template (requer load_state e REPO_URL)
render_user_data() {
  sed -e "s|__REPO_URL__|$REPO_URL|g" \
      -e "s|__DB_CONNECTION__|Host=$DB_ENDPOINT;Port=5432;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD;SSL Mode=Require;Trust Server Certificate=true|g" \
      -e "s|__REDIS_CONNECTION__|$REDIS_ENDPOINT:6379|g" \
      -e "s|__AWS_REGION__|$REGION|g" \
      -e "s|__S3_BUCKET__|$S3_BUCKET|g" \
      -e "s|__SNS_TOPIC_ARN__|$SNS_TOPIC_ARN|g" \
      -e "s|__SQS_QUEUE_URL__|$SQS_QUEUE_URL|g" \
      -e "s|__DYNAMODB_TABLE__|$TABLE|g" \
      "$(dirname "${BASH_SOURCE[0]}")/user-data.sh" > "$1"
}
