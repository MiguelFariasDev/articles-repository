#!/bin/bash
# Uso: REPO_URL=https://github.com/<usuario>/articles-repository.git ./07-create-ec2.sh
source "$(dirname "$0")/00-common.sh"
load_state

: "${REPO_URL:?Defina REPO_URL (ex.: REPO_URL=https://github.com/<usuario>/articles-repository.git)}"
for v in DB_ENDPOINT DB_PASSWORD REDIS_ENDPOINT S3_BUCKET SNS_TOPIC_ARN SQS_QUEUE_URL IAM_INSTANCE_PROFILE; do
  [ -n "${!v:-}" ] || { echo "Falta $v em .state.env — rode os scripts 01 a 06 antes." >&2; exit 1; }
done

SG_EC2="$(ensure_sg "$SG_EC2_NAME" "Articles Repository EC2")"
MY_IP="${MY_IP:-$(curl -s https://checkip.amazonaws.com)}"
log "Liberando SSH (22) para $MY_IP/32 e API (5000)"
allow_from_cidr "$SG_EC2" 22 "$MY_IP/32"
allow_from_cidr "$SG_EC2" 5000 "0.0.0.0/0"
save_state SG_EC2 "$SG_EC2"

EXISTING="$(aws ec2 describe-instances \
  --filters Name=tag:Name,Values="${PROJECT}-app" Name=instance-state-name,Values=pending,running \
  --query 'Reservations[0].Instances[0].InstanceId' --output text)"
if [ "$EXISTING" != "None" ]; then
  log "Instância já existe: $EXISTING"
  INSTANCE_ID="$EXISTING"
else
  AMI="$(aws ssm get-parameter --name /aws/service/ami-amazon-linux-latest/al2023-ami-kernel-default-x86_64 \
         --query Parameter.Value --output text)"

  # Renderiza o user-data substituindo os placeholders (sem segredos no repositório).
  USER_DATA="$(mktemp)"; trap 'rm -f "$USER_DATA"' EXIT
  render_user_data "$USER_DATA"

  log "Criando EC2 t3.micro (Amazon Linux 2023, AMI $AMI)"
  INSTANCE_ID="$(aws ec2 run-instances \
    --image-id "$AMI" --instance-type t3.micro \
    --security-group-ids "$SG_EC2" \
    ${KEY_NAME:+--key-name "$KEY_NAME"} \
    --iam-instance-profile Name="$IAM_INSTANCE_PROFILE" \
    --user-data "file://$USER_DATA" \
    --tag-specifications "ResourceType=instance,Tags=[{Key=Name,Value=${PROJECT}-app}]" \
    --query 'Instances[0].InstanceId' --output text)"
fi
save_state EC2_INSTANCE_ID "$INSTANCE_ID"

aws ec2 wait instance-running --instance-ids "$INSTANCE_ID"
IP="$(aws ec2 describe-instances --instance-ids "$INSTANCE_ID" --query 'Reservations[0].Instances[0].PublicIpAddress' --output text)"
save_state EC2_PUBLIC_IP "$IP"
log "OK: instância $INSTANCE_ID"
echo "  API   : http://$IP:5000/swagger (após o bootstrap, ~3-5 min)"
echo "  Health: http://$IP:5000/health"
