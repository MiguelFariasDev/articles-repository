#!/bin/bash
# Parte 2 — Launch Template + Auto Scaling Group (min=1, desired=1, max=3) + escala por CPU
# (70% → +1 instância, 25% → -1 instância). Uso: REPO_URL=https://github.com/<usuario>/articles-repository.git ./09-create-asg.sh
source "$(dirname "$0")/00-common.sh"
load_state

: "${REPO_URL:?Defina REPO_URL (ex.: REPO_URL=https://github.com/<usuario>/articles-repository.git)}"
for v in DB_ENDPOINT DB_PASSWORD REDIS_ENDPOINT S3_BUCKET SNS_TOPIC_ARN SQS_QUEUE_URL IAM_INSTANCE_PROFILE SG_EC2 TG_ARN; do
  [ -n "${!v:-}" ] || { echo "Falta $v em .state.env — rode os scripts 01 a 08 antes." >&2; exit 1; }
done

# 1. Launch Template (mesmo user-data da Parte 1; cada instância sobe API + Worker)
AMI="$(aws ssm get-parameter --name /aws/service/ami-amazon-linux-latest/al2023-ami-kernel-default-x86_64 \
       --query Parameter.Value --output text)"
USER_DATA="$(mktemp)"; trap 'rm -f "$USER_DATA"' EXIT
render_user_data "$USER_DATA"

KEY_JSON=""; [ -z "${KEY_NAME:-}" ] || KEY_JSON="\"KeyName\": \"$KEY_NAME\","
LT_DATA="$(mktemp)"; trap 'rm -f "$USER_DATA" "$LT_DATA"' EXIT
cat >"$LT_DATA" <<JSON
{
  "ImageId": "$AMI",
  "InstanceType": "t3.micro",
  "IamInstanceProfile": {"Name": "$IAM_INSTANCE_PROFILE"},
  "SecurityGroupIds": ["$SG_EC2"],
  $KEY_JSON
  "Monitoring": {"Enabled": true},
  "UserData": "$(base64 -w0 "$USER_DATA")",
  "TagSpecifications": [{"ResourceType": "instance", "Tags": [{"Key": "Name", "Value": "${PROJECT}-asg-instance"}]}]
}
JSON

if aws ec2 describe-launch-templates --launch-template-names "$LT_NAME" >/dev/null 2>&1; then
  log "Launch template existe — criando nova versão e definindo como padrão"
  VER="$(aws ec2 create-launch-template-version --launch-template-name "$LT_NAME" \
         --launch-template-data "file://$LT_DATA" --query 'LaunchTemplateVersion.VersionNumber' --output text)"
  aws ec2 modify-launch-template --launch-template-name "$LT_NAME" --default-version "$VER" >/dev/null
else
  log "Criando launch template $LT_NAME (t3.micro, AL2023)"
  aws ec2 create-launch-template --launch-template-name "$LT_NAME" \
    --launch-template-data "file://$LT_DATA" >/dev/null
fi

# 2. Auto Scaling Group
SUBNET_LIST="$(default_subnets | tr -s '[:space:]' ',' | sed 's/,$//')"
if [ "$(aws autoscaling describe-auto-scaling-groups --auto-scaling-group-names "$ASG_NAME" \
        --query 'length(AutoScalingGroups)' --output text)" = "0" ]; then
  log "Criando ASG $ASG_NAME (min=1, desired=1, max=3)"
  aws autoscaling create-auto-scaling-group --auto-scaling-group-name "$ASG_NAME" \
    --launch-template "LaunchTemplateName=$LT_NAME,Version=\$Default" \
    --min-size 1 --desired-capacity 1 --max-size 3 \
    --vpc-zone-identifier "$SUBNET_LIST" \
    --target-group-arns "$TG_ARN" \
    --health-check-type ELB --health-check-grace-period 600 \
    --default-instance-warmup 300 \
    --tags "Key=Name,Value=${PROJECT}-asg-instance,PropagateAtLaunch=true"
else
  log "ASG já existe — atualizando para a versão padrão do launch template"
  aws autoscaling update-auto-scaling-group --auto-scaling-group-name "$ASG_NAME" \
    --launch-template "LaunchTemplateName=$LT_NAME,Version=\$Default" \
    --min-size 1 --max-size 3
fi

# 3. Políticas de escala (step scaling) + alarmes CloudWatch: >70% escala, <25% reduz (média por 2 períodos de 1 min)
OUT_ARN="$(aws autoscaling put-scaling-policy --auto-scaling-group-name "$ASG_NAME" \
  --policy-name "${PROJECT}-scale-out" --policy-type SimpleScaling \
  --adjustment-type ChangeInCapacity --scaling-adjustment 1 --cooldown 300 \
  --query PolicyARN --output text)"
IN_ARN="$(aws autoscaling put-scaling-policy --auto-scaling-group-name "$ASG_NAME" \
  --policy-name "${PROJECT}-scale-in" --policy-type SimpleScaling \
  --adjustment-type ChangeInCapacity --scaling-adjustment -1 --cooldown 300 \
  --query PolicyARN --output text)"

aws cloudwatch put-metric-alarm --alarm-name "${PROJECT}-cpu-high" \
  --namespace AWS/EC2 --metric-name CPUUtilization \
  --dimensions Name=AutoScalingGroupName,Value="$ASG_NAME" \
  --statistic Average --period 60 --evaluation-periods 2 \
  --threshold 70 --comparison-operator GreaterThanThreshold \
  --alarm-actions "$OUT_ARN"
aws cloudwatch put-metric-alarm --alarm-name "${PROJECT}-cpu-low" \
  --namespace AWS/EC2 --metric-name CPUUtilization \
  --dimensions Name=AutoScalingGroupName,Value="$ASG_NAME" \
  --statistic Average --period 60 --evaluation-periods 2 \
  --threshold 25 --comparison-operator LessThanThreshold \
  --alarm-actions "$IN_ARN"

save_state ASG_NAME "$ASG_NAME"
log "OK: ASG $ASG_NAME criado. ALB: http://${ALB_DNS:-<ver 08>}  (bootstrap ~5 min; /health deve responder 200)"
log "Se a instância avulsa da Parte 1 ainda existe, encerre-a: ela não faz parte do ASG."
