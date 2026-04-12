#!/bin/bash

ENDPOINT="http://localhost:4566"
REGION="us-east-1"

echo "Creating SNS topic..."
aws sns create-topic \
  --name smartcommerce-orders \
  --endpoint-url $ENDPOINT \
  --region $REGION

echo "Creating SQS queue (main)..."
aws sqs create-queue \
  --queue-name smartcommerce-orders-queue \
  --endpoint-url $ENDPOINT \
  --region $REGION

echo "Creating SQS dead-letter queue..."
aws sqs create-queue \
  --queue-name smartcommerce-orders-dlq \
  --endpoint-url $ENDPOINT \
  --region $REGION

# Get ARNs
TOPIC_ARN=$(aws sns list-topics \
  --endpoint-url $ENDPOINT \
  --region $REGION \
  --query "Topics[?ends_with(TopicArn, 'smartcommerce-orders')].TopicArn" \
  --output text)

QUEUE_URL=$(aws sqs get-queue-url \
  --queue-name smartcommerce-orders-queue \
  --endpoint-url $ENDPOINT \
  --region $REGION \
  --query QueueUrl \
  --output text)

QUEUE_ARN=$(aws sqs get-queue-attributes \
  --queue-url $QUEUE_URL \
  --attribute-names QueueArn \
  --endpoint-url $ENDPOINT \
  --region $REGION \
  --query Attributes.QueueArn \
  --output text)

echo "Subscribing SQS queue to SNS topic..."
aws sns subscribe \
  --topic-arn $TOPIC_ARN \
  --protocol sqs \
  --notification-endpoint $QUEUE_ARN \
  --endpoint-url $ENDPOINT \
  --region $REGION

echo ""
echo "Done. Resources created:"
echo "  SNS Topic ARN : $TOPIC_ARN"
echo "  SQS Queue URL : $QUEUE_URL"
echo "  SQS Queue ARN : $QUEUE_ARN"