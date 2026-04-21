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

echo "Creating SQS queue (notifications)..."
aws sqs create-queue \
  --queue-name smartcommerce-notifications-queue \
  --endpoint-url $ENDPOINT \
  --region $REGION

echo "Creating SQS dead-letter queue (notifications)..."
aws sqs create-queue \
  --queue-name smartcommerce-notifications-dlq \
  --endpoint-url $ENDPOINT \
  --region $REGION

NOTIFICATIONS_QUEUE_URL=$(aws sqs get-queue-url \
  --queue-name smartcommerce-notifications-queue \
  --endpoint-url $ENDPOINT \
  --region $REGION \
  --query QueueUrl \
  --output text)

NOTIFICATIONS_QUEUE_ARN=$(aws sqs get-queue-attributes \
  --queue-url $NOTIFICATIONS_QUEUE_URL \
  --attribute-names QueueArn \
  --endpoint-url $ENDPOINT \
  --region $REGION \
  --query Attributes.QueueArn \
  --output text)

echo "Subscribing notifications queue to SNS topic..."
aws sns subscribe \
  --topic-arn $TOPIC_ARN \
  --protocol sqs \
  --notification-endpoint $NOTIFICATIONS_QUEUE_ARN \
  --endpoint-url $ENDPOINT \
  --region $REGION

echo "  Notifications Queue URL : $NOTIFICATIONS_QUEUE_URL"
echo "  Notifications Queue ARN : $NOTIFICATIONS_QUEUE_ARN"


echo "Creating SQS queue (inventory)..."
aws sqs create-queue \
  --queue-name smartcommerce-inventory-queue \
  --endpoint-url $ENDPOINT \
  --region $REGION

echo "Creating SQS dead-letter queue (inventory)..."
aws sqs create-queue \
  --queue-name smartcommerce-inventory-dlq \
  --endpoint-url $ENDPOINT \
  --region $REGION

INVENTORY_QUEUE_URL=$(aws sqs get-queue-url \
  --queue-name smartcommerce-inventory-queue \
  --endpoint-url $ENDPOINT \
  --region $REGION \
  --query QueueUrl \
  --output text)

INVENTORY_QUEUE_ARN=$(aws sqs get-queue-attributes \
  --queue-url $INVENTORY_QUEUE_URL \
  --attribute-names QueueArn \
  --endpoint-url $ENDPOINT \
  --region $REGION \
  --query Attributes.QueueArn \
  --output text)

echo "Subscribing inventory queue to SNS topic..."
aws sns subscribe \
  --topic-arn $TOPIC_ARN \
  --protocol sqs \
  --notification-endpoint $INVENTORY_QUEUE_ARN \
  --endpoint-url $ENDPOINT \
  --region $REGION

echo "  Inventory Queue URL : $INVENTORY_QUEUE_URL"
echo "  Inventory Queue ARN : $INVENTORY_QUEUE_ARN"