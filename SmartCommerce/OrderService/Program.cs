using Amazon.DynamoDBv2;
using Amazon.Extensions.NETCore.Setup;
using OrderService.Application.Interfaces;
using OrderService.Infrastructure.DynamoDB;
using Serilog;
using FluentValidation;
using OrderService.Application.Behaviours;
using MediatR;
using Amazon.SimpleNotificationService;
using OrderService.Infrastructure.Outbox;
using OrderService.Infrastructure.Sns;
using Amazon.SQS;
using OrderService.Infrastructure.Sqs;
using OrderService.Domain.Orders.Events;
using OrderService.Application.EventHandlers;
using OrderService.Infrastructure.Correlation;
using OrderService.Infrastructure.Idempotency;
using OrderService.Application.Dispatching;
using OrderService.Infrastructure.Tenancy;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using OrderService.Infrastructure.Health;
using OrderService.Infrastructure.AI;
using Amazon.BedrockRuntime;

// ─── Bootstrap logger (catches startup errors) ───────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting OrderService...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Idempotency
    builder.Services.Configure<IdempotencySettings>(
        builder.Configuration.GetSection(IdempotencySettings.SectionName));
    builder.Services.AddScoped<IdempotencyGuard>();

    // ─── Serilog ─────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .Enrich.WithProperty("Service", "OrderService")
           );


    builder.Services.AddScoped<CorrelationIdAccessor>();

    builder.Services.AddScoped<TenantContext>();
    builder.Services.AddScoped<ITenantContext>(sp => sp.GetRequiredService<TenantContext>());

    // ─── AWS + DynamoDB ───────────────────────────────────────────────────────    
    builder.Services.Configure<DynamoDbSettings>(builder.Configuration.GetSection(DynamoDbSettings.SectionName));
    builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
    {
        var settings = builder.Configuration
            .GetSection(DynamoDbSettings.SectionName)
            .Get<DynamoDbSettings>();

        var config = new AmazonDynamoDBConfig
        {
            RegionEndpoint = Amazon.RegionEndpoint.USEast1
        };

        // Use local endpoint if configured (dev/test)
        if (!string.IsNullOrEmpty(settings?.ServiceURL))
            config.ServiceURL = settings.ServiceURL;

        return new AmazonDynamoDBClient("local", "local", config);
    });
    builder.Services.AddScoped<IOrderRepository, DynamoDbOrderRepository>();

    // SNS client
    builder.Services.Configure<SnsSettings>(
        builder.Configuration.GetSection(SnsSettings.SectionName));

    builder.Services.AddSingleton<IAmazonSimpleNotificationService>(_ =>
    {
        var settings = builder.Configuration
            .GetSection(SnsSettings.SectionName)
            .Get<SnsSettings>();

        var config = new AmazonSimpleNotificationServiceConfig
        {
            RegionEndpoint = Amazon.RegionEndpoint.USEast1
        };

        if (!string.IsNullOrEmpty(settings?.ServiceURL))
            config.ServiceURL = settings.ServiceURL;

        return new AmazonSimpleNotificationServiceClient("local", "local", config);
    });

    // ─── Bedrock / AI ─────────────────────────────────────────────────────────
    builder.Services.Configure<BedrockOptions>(
        builder.Configuration.GetSection("Bedrock"));

    var useStub = builder.Configuration.GetValue<bool>("Bedrock:UseStub");
    if (useStub)
    {
        builder.Services.AddSingleton<IAnomalyDetectionService, StubAnomalyDetectionService>();
    }
    else
    {
        builder.Services.AddAWSService<IAmazonBedrockRuntime>();
        builder.Services.AddSingleton<IAnomalyDetectionService, BedrockAnomalyDetectionService>();
    }

    // SQS client
    builder.Services.Configure<SqsSettings>(
        builder.Configuration.GetSection(SqsSettings.SectionName));

    builder.Services.AddSingleton<IAmazonSQS>(_ =>
    {
        var settings = builder.Configuration
            .GetSection(SqsSettings.SectionName)
            .Get<SqsSettings>();

        var config = new AmazonSQSConfig
        {
            RegionEndpoint = Amazon.RegionEndpoint.USEast1
        };

        if (!string.IsNullOrEmpty(settings?.ServiceURL))
            config.ServiceURL = settings.ServiceURL;

        return new AmazonSQSClient("local", "local", config);
    });

    // Event handlers   

    new EventDispatcherBuilder(builder.Services)
        .Register<OrderPlacedEvent, OrderPlacedEventHandler>("order.placed")
        .Register<OrderCancelledEvent, OrderCancelledEventHandler>("order.cancelled")
        .Build();

    // SQS consumer — runs in background
    builder.Services.AddHostedService<SqsEventConsumer>();

    builder.Services.AddScoped<IEventPublisher, SnsEventPublisher>();

    // Outbox processor — runs in background
    builder.Services.AddHostedService<OutboxProcessor>();

    // ─── App Services ─────────────────────────────────────────────────────────
    builder.Services.AddControllers();

    builder.Services.Configure<Microsoft.AspNetCore.Mvc.ApiBehaviorOptions>(options =>
    options.SuppressModelStateInvalidFilter = true);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title   = "SmartCommerce - Order Service",
            Version = "v1",
            Description = "Multi-tenant Order Management API"
        });
    });

    //_____MEdiatR__________________________
    // MediatR — scans assembly for all handlers
    builder.Services.AddMediatR(typeof(Program).Assembly);

    // Validation pipeline — runs before every handler
    builder.Services.AddTransient(
        typeof(IPipelineBehavior<,>),
        typeof(ValidationBehaviour<,>));

    // FluentValidation — scans assembly for all validators
    builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

    builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(AnomalyDetectionBehaviour<,>));

    // ─── Health Checks ────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
    .AddCheck<DynamoDbLivenessCheck>("dynamodb")
    .AddCheck("sns", () => HealthCheckResult.Healthy("SNS configured"));

    var app = builder.Build();

    // ─── Middleware Pipeline ──────────────────────────────────────────────────
   app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0000}ms)";
        opts.EnrichDiagnosticContext = (diagCtx, httpCtx) =>
        {
            diagCtx.Set("CorrelationId",
                httpCtx.Response.Headers["X-Correlation-Id"].FirstOrDefault() ?? string.Empty);
        };
    });

    app.UseMiddleware<CorrelationIdMiddleware>();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<TenantMiddleware>();   // ← add this line

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service v1");
            c.RoutePrefix = string.Empty; // Swagger at root
        });
    }

    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        ResponseWriter = async (context, report) =>
        {
            context.Response.ContentType = "application/json";

            var result = new
            {
                status  = report.Status.ToString(),
                service = "OrderService",
                checks  = report.Entries.Select(e => new
                {
                    name     = e.Key,
                    status   = e.Value.Status.ToString(),
                    duration = e.Value.Duration.TotalMilliseconds
                }),
                totalDuration = report.TotalDuration.TotalMilliseconds
            };

            await context.Response.WriteAsJsonAsync(result);
        }
    });

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "OrderService failed to start");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}


return 0;

// Make Program visible to integration tests
public partial class Program { }
