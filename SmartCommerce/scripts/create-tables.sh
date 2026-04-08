#!/bin/bash

ENDPOINT="http://localhost:8000"
REGION="us-east-1"
AWS_PROFILE="local"

echo "Creating SmartCommerce Orders table..."

aws dynamodb create-table \
  --endpoint-url $ENDPOINT \
  --region $REGION \
  --table-name SmartCommerce_Orders \
  --attribute-definitions \
    AttributeName=PK,AttributeType=S \
    AttributeName=SK,AttributeType=S \
    AttributeName=GSI1PK,AttributeType=S \
    AttributeName=GSI1SK,AttributeType=S \
  --key-schema \
    AttributeName=PK,KeyType=HASH \
    AttributeName=SK,KeyType=RANGE \
  --global-secondary-indexes \
    "[{
      \"IndexName\": \"GSI1\",
      \"KeySchema\": [
        {\"AttributeName\": \"GSI1PK\", \"KeyType\": \"HASH\"},
        {\"AttributeName\": \"GSI1SK\", \"KeyType\": \"RANGE\"}
      ],
      \"Projection\": {\"ProjectionType\": \"ALL\"},
      \"ProvisionedThroughput\": {\"ReadCapacityUnits\": 5, \"WriteCapacityUnits\": 5}
    }]" \
  --provisioned-throughput ReadCapacityUnits=5,WriteCapacityUnits=5

echo "Creating SmartCommerce Outbox table..."

aws dynamodb create-table \
  --endpoint-url $ENDPOINT \
  --region $REGION \
  --table-name SmartCommerce_Outbox \
  --attribute-definitions \
    AttributeName=PK,AttributeType=S \
    AttributeName=SK,AttributeType=S \
    AttributeName=GSI1PK,AttributeType=S \
    AttributeName=GSI1SK,AttributeType=S \
  --key-schema \
    AttributeName=PK,KeyType=HASH \
    AttributeName=SK,KeyType=RANGE \
  --global-secondary-indexes \
    "[{
      \"IndexName\": \"GSI1\",
      \"KeySchema\": [
        {\"AttributeName\": \"GSI1PK\", \"KeyType\": \"HASH\"},
        {\"AttributeName\": \"GSI1SK\", \"KeyType\": \"RANGE\"}
      ],
      \"Projection\": {\"ProjectionType\": \"ALL\"},
      \"ProvisionedThroughput\": {\"ReadCapacityUnits\": 5, \"WriteCapacityUnits\": 5}
    }]" \
  --provisioned-throughput ReadCapacityUnits=5,WriteCapacityUnits=5

echo "Done. Tables created."