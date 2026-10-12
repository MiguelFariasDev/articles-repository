#!/bin/bash
# Executado automaticamente pelo LocalStack (ready.d) quando o serviço fica pronto.
set -euo pipefail

BUCKET=articles-repository-pdfs
TOPIC=articles-repository-topic
QUEUE=articles-repository-queue
DLQ=articles-repository-queue-dlq
TABLE=articles-repository-logs
REGION=us-east-1

echo "[init] S3"
awslocal s3 mb "s3://$BUCKET"
awslocal s3api put-bucket-versioning --bucket "$BUCKET" --versioning-configuration Status=Enabled

echo "[init] SNS"
TOPIC_ARN=$(awslocal sns create-topic --name "$TOPIC" --query TopicArn --output text)

echo "[init] SQS (fila + DLQ + redrive)"
DLQ_URL=$(awslocal sqs create-queue --queue-name "$DLQ" --query QueueUrl --output text)
DLQ_ARN=$(awslocal sqs get-queue-attributes --queue-url "$DLQ_URL" --attribute-names QueueArn --query Attributes.QueueArn --output text)

cat > /tmp/queue-attrs.json <<JSON
{
  "VisibilityTimeout": "300",
  "RedrivePolicy": "{\"deadLetterTargetArn\":\"$DLQ_ARN\",\"maxReceiveCount\":\"3\"}"
}
JSON
QUEUE_URL=$(awslocal sqs create-queue --queue-name "$QUEUE" --attributes file:///tmp/queue-attrs.json --query QueueUrl --output text)
QUEUE_ARN=$(awslocal sqs get-queue-attributes --queue-url "$QUEUE_URL" --attribute-names QueueArn --query Attributes.QueueArn --output text)

echo "[init] Subscription SNS -> SQS"
awslocal sns subscribe --topic-arn "$TOPIC_ARN" --protocol sqs --notification-endpoint "$QUEUE_ARN" >/dev/null

echo "[init] DynamoDB"
awslocal dynamodb create-table \
  --table-name "$TABLE" \
  --attribute-definitions AttributeName=Id,AttributeType=S AttributeName=Timestamp,AttributeType=S \
  --key-schema AttributeName=Id,KeyType=HASH AttributeName=Timestamp,KeyType=RANGE \
  --billing-mode PAY_PER_REQUEST >/dev/null
awslocal dynamodb update-time-to-live --table-name "$TABLE" \
  --time-to-live-specification "Enabled=true,AttributeName=Ttl" >/dev/null

echo "[init] concluído: bucket=$BUCKET topic=$TOPIC_ARN queue=$QUEUE_URL table=$TABLE"
