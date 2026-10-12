#!/bin/bash
# REMOVE todos os recursos criados pelos scripts 01–09. Irreversível (RDS sem snapshot final, bucket esvaziado).
source "$(dirname "$0")/00-common.sh"

if [ "${FORCE:-}" != "1" ]; then
  read -r -p "Isso apagará TODA a infra do projeto '$PROJECT' na conta $ACCOUNT_ID ($REGION). Digite '$PROJECT' para confirmar: " ans
  [ "$ans" = "$PROJECT" ] || { echo "Cancelado."; exit 1; }
fi
set +e   # limpeza segue mesmo que algum recurso já não exista

log "Auto Scaling / ALB"
aws cloudwatch delete-alarms --alarm-names "${PROJECT}-cpu-high" "${PROJECT}-cpu-low" 2>/dev/null
if [ "$(aws autoscaling describe-auto-scaling-groups --auto-scaling-group-names "$ASG_NAME" --query 'length(AutoScalingGroups)' --output text 2>/dev/null)" = "1" ]; then
  aws autoscaling delete-auto-scaling-group --auto-scaling-group-name "$ASG_NAME" --force-delete
  while [ "$(aws autoscaling describe-auto-scaling-groups --auto-scaling-group-names "$ASG_NAME" --query 'length(AutoScalingGroups)' --output text)" != "0" ]; do sleep 10; done
fi
aws ec2 delete-launch-template --launch-template-name "$LT_NAME" >/dev/null 2>&1
ALB_ARN="$(aws elbv2 describe-load-balancers --names "$ALB_NAME" --query 'LoadBalancers[0].LoadBalancerArn' --output text 2>/dev/null)"
if [ -n "$ALB_ARN" ] && [ "$ALB_ARN" != "None" ]; then
  aws elbv2 delete-load-balancer --load-balancer-arn "$ALB_ARN"
  aws elbv2 wait load-balancers-deleted --load-balancer-arns "$ALB_ARN"
fi
TG_ARN="$(aws elbv2 describe-target-groups --names "$TG_NAME" --query 'TargetGroups[0].TargetGroupArn' --output text 2>/dev/null)"
[ -n "$TG_ARN" ] && [ "$TG_ARN" != "None" ] && aws elbv2 delete-target-group --target-group-arn "$TG_ARN"

log "EC2"
IDS="$(aws ec2 describe-instances --filters Name=tag:Name,Values="${PROJECT}-app" Name=instance-state-name,Values=pending,running,stopped,stopping \
       --query 'Reservations[].Instances[].InstanceId' --output text)"
if [ -n "$IDS" ]; then aws ec2 terminate-instances --instance-ids $IDS >/dev/null; aws ec2 wait instance-terminated --instance-ids $IDS; fi

log "IAM"
aws iam remove-role-from-instance-profile --instance-profile-name "$PROFILE_NAME" --role-name "$ROLE_NAME" 2>/dev/null
aws iam delete-instance-profile --instance-profile-name "$PROFILE_NAME" 2>/dev/null
for policy in AmazonS3FullAccess AmazonSNSFullAccess AmazonSQSFullAccess AmazonDynamoDBFullAccess; do
  aws iam detach-role-policy --role-name "$ROLE_NAME" --policy-arn "arn:aws:iam::aws:policy/$policy" 2>/dev/null
done
aws iam delete-role --role-name "$ROLE_NAME" 2>/dev/null

log "SNS / SQS"
TOPIC_ARN="$(aws sns list-topics --query "Topics[?ends_with(TopicArn, ':$TOPIC_NAME')].TopicArn | [0]" --output text)"
[ -n "$TOPIC_ARN" ] && [ "$TOPIC_ARN" != "None" ] && aws sns delete-topic --topic-arn "$TOPIC_ARN"
for q in "$QUEUE_NAME" "$DLQ_NAME"; do
  url="$(aws sqs get-queue-url --queue-name "$q" --query QueueUrl --output text 2>/dev/null)"
  [ -n "$url" ] && aws sqs delete-queue --queue-url "$url"
done

log "DynamoDB"
aws dynamodb delete-table --table-name "$TABLE" >/dev/null 2>&1

log "ElastiCache"
aws elasticache delete-cache-cluster --cache-cluster-id "$REDIS_ID" >/dev/null 2>&1 &&
  aws elasticache wait cache-cluster-deleted --cache-cluster-id "$REDIS_ID"

log "RDS"
aws rds delete-db-instance --db-instance-identifier "$DB_ID" --skip-final-snapshot --delete-automated-backups >/dev/null 2>&1 &&
  aws rds wait db-instance-deleted --db-instance-identifier "$DB_ID"

log "S3 (esvaziando bucket versionado)"
if aws s3api head-bucket --bucket "$BUCKET" 2>/dev/null; then
  aws s3 rm "s3://$BUCKET" --recursive >/dev/null
  # remove versões e delete markers restantes
  for kind in Versions DeleteMarkers; do
    aws s3api list-object-versions --bucket "$BUCKET" --query "$kind[].[Key,VersionId]" --output text 2>/dev/null |
      while read -r key vid; do
        [ -n "$key" ] && [ "$key" != "None" ] && aws s3api delete-object --bucket "$BUCKET" --key "$key" --version-id "$vid" >/dev/null
      done
  done
  aws s3api delete-bucket --bucket "$BUCKET"
fi

log "Security groups"
sleep 20   # aguarda as ENIs do ALB/instâncias serem liberadas
for name in "$SG_RDS_NAME" "$SG_REDIS_NAME" "$SG_EC2_NAME" "$SG_ALB_NAME"; do
  id="$(aws ec2 describe-security-groups --filters Name=group-name,Values="$name" --query 'SecurityGroups[0].GroupId' --output text)"
  [ -n "$id" ] && [ "$id" != "None" ] && aws ec2 delete-security-group --group-id "$id"
done

rm -f "$STATE_FILE"
log "Limpeza concluída."
