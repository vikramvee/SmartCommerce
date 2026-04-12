#!/bin/bash

echo "Starting SmartCommerce dev environment..."

docker compose up -d dynamodb-local

echo "Waiting for DynamoDB Local to be healthy..."
until [ "$(docker inspect --format='{{.State.Health.Status}}' smartcommerce-dynamo)" = "healthy" ]; do
  sleep 2
done

echo "Creating tables..."
bash scripts/create-tables.sh

echo "Seeding data..."
bash scripts/seed-data.sh

echo "Creating SNS/SQS resources..."
bash scripts/create-sns-sqs.sh

docker compose up -d

echo "Dev environment ready!"
echo "  DynamoDB Local:  http://localhost:8000"
echo "  DynamoDB Admin:  http://localhost:8001"
echo "  LocalStack     : http://localhost:4566"