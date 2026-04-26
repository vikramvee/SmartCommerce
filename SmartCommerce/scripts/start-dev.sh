#!/bin/bash

echo "Starting SmartCommerce dev environment..."

docker compose up -d dynamodb-local localstack

echo "Waiting for DynamoDB Local to be healthy..."
until [ "$(docker inspect --format='{{.State.Health.Status}}' smartcommerce-dynamo)" = "healthy" ]; do
  sleep 2
done

echo "Creating tables..."
bash scripts/create-tables.sh

echo "Seeding data..."
bash scripts/seed-data.sh

echo "Waiting for LocalStack SNS to be ready..."
until aws sns list-topics --endpoint-url http://localhost:4566 --region us-east-1 > /dev/null 2>&1; do
  echo "  LocalStack not ready yet, retrying in 3s..."
  sleep 3
done
echo "LocalStack ready."

echo "Creating SNS/SQS resources..."
bash scripts/create-sns-sqs.sh

#docker compose up -d

echo ""
echo "✅ Dev environment ready!"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo "  DynamoDB Local:  http://localhost:8000"
echo "  DynamoDB Admin:  http://localhost:8001"
echo "  LocalStack:      http://localhost:4566"
echo "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━"
echo ""
echo "▶ Start services in separate terminals:"
echo ""
echo "  Terminal 1 — OrderService:"
echo "  dotnet run --project SmartCommerce/OrderService/OrderService.csproj"
echo ""
echo "  Terminal 2 — InventoryService:"
echo "  dotnet run --project SmartCommerce/InventoryService/InventoryService.csproj"
echo ""
echo "  Terminal 3 — NotificationService (if needed):"
echo "  dotnet run --project SmartCommerce/NotificationService/NotificationService.csproj"
echo ""