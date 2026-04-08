#!/bin/bash

ENDPOINT="http://localhost:8000"
REGION="us-east-1"

echo "Seeding test orders..."

aws dynamodb put-item \
  --endpoint-url $ENDPOINT \
  --region $REGION \
  --table-name SmartCommerce_Orders \
  --item '{
    "PK":       {"S": "TENANT#tenant-alpha"},
    "SK":       {"S": "ORDER#order-001"},
    "GSI1PK":   {"S": "TENANT#tenant-alpha#STATUS#PENDING"},
    "GSI1SK":   {"S": "ORDER#order-001"},
    "OrderId":  {"S": "order-001"},
    "TenantId": {"S": "tenant-alpha"},
    "Status":   {"S": "PENDING"},
    "Total":    {"N": "149.99"},
    "CreatedAt":{"S": "2026-04-08T00:00:00Z"},
    "EntityType":{"S": "ORDER"}
  }'

aws dynamodb put-item \
  --endpoint-url $ENDPOINT \
  --region $REGION \
  --table-name SmartCommerce_Orders \
  --item '{
    "PK":       {"S": "TENANT#tenant-beta"},
    "SK":       {"S": "ORDER#order-002"},
    "GSI1PK":   {"S": "TENANT#tenant-beta#STATUS#CONFIRMED"},
    "GSI1SK":   {"S": "ORDER#order-002"},
    "OrderId":  {"S": "order-002"},
    "TenantId": {"S": "tenant-beta"},
    "Status":   {"S": "CONFIRMED"},
    "Total":    {"N": "299.50"},
    "CreatedAt":{"S": "2026-04-08T01:00:00Z"},
    "EntityType":{"S": "ORDER"}
  }'

echo "Seed complete."