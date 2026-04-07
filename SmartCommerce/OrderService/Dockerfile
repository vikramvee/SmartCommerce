# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and project files
COPY OrderService/OrderService.csproj OrderService/
COPY InventoryService/InventoryService.csproj InventoryService/
COPY NotificationService/NotificationService.csproj NotificationService/
COPY SmartCommerce.Contracts/SmartCommerce.Contracts.csproj SmartCommerce.Contracts/

# Restore dependencies
RUN dotnet restore "OrderService/OrderService.csproj" \
    --packages /root/.nuget/packages

# Copy everything else
COPY . .

# Build and publish
RUN dotnet publish OrderService/OrderService.csproj \
    -c Release -o /app/publish

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0-preview AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "OrderService.dll"]