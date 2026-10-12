#!/bin/bash
source "$(dirname "$0")/00-common.sh"
TMP="$(mktemp -d)"; trap 'rm -rf "$TMP"' EXIT

log "SNS: tópico $TOPIC_NAME"
TOPIC_ARN="$(aws sns create-topic --name "$TOPIC_NAME" --query TopicArn --output text)"   # idempotente

log "SQS: DLQ $DLQ_NAME"
DLQ_URL="$(aws sqs create-queue --queue-name "$DLQ_NAME" --query QueueUrl --output text)"
DLQ_ARN="$(aws sqs get-queue-attributes --queue-url "$DLQ_URL" --attribute-names QueueArn --query Attributes.QueueArn --output text)"

log "SQS: fila $QUEUE_NAME (redrive maxReceiveCount=3)"
cat > "$TMP/attrs.json" <<JSON
{
  "VisibilityTimeout": "300",
  "RedrivePolicy": "{\"deadLetterTargetArn\":\"$DLQ_ARN\",\"maxReceiveCount\":\"3\"}"
}
JSON
QUEUE_URL="$(aws sqs create-queue --queue-name "$QUEUE_NAME" --attributes "file://$TMP/attrs.json" --query QueueUrl --output text)"
QUEUE_ARN="$(aws sqs get-queue-attributes --queue-url "$QUEUE_URL" --attribute-names QueueArn --query Attributes.QueueArn --output text)"

log "SQS: permitindo que o tópico publique na fila"
cat > "$TMP/policy.json" <<JSON
{
  "Policy": "{\"Version\":\"2012-10-17\",\"Statement\":[{\"Effect\":\"Allow\",\"Principal\":{\"Service\":\"sns.amazonaws.com\"},\"Action\":\"sqs:SendMessage\",\"Resource\":\"$QUEUE_ARN\",\"Condition\":{\"ArnEquals\":{\"aws:SourceArn\":\"$TOPIC_ARN\"}}}]}"
}
JSON
aws sqs set-queue-attributes --queue-url "$QUEUE_URL" --attributes "file://$TMP/policy.json"

log "SNS -> SQS: subscription"
aws sns subscribe --topic-arn "$TOPIC_ARN" --protocol sqs --notification-endpoint "$QUEUE_ARN" >/dev/null   # idempotente

save_state SNS_TOPIC_ARN "$TOPIC_ARN"
save_state SQS_QUEUE_URL "$QUEUE_URL"
save_state SQS_QUEUE_ARN "$QUEUE_ARN"
save_state SQS_DLQ_ARN "$DLQ_ARN"
log "OK"
echo "  Topic ARN : $TOPIC_ARN"
echo "  Queue URL : $QUEUE_URL"
echo "  Queue ARN : $QUEUE_ARN"
echo "  DLQ ARN   : $DLQ_ARN"
