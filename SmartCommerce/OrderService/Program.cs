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

// ─── Bootstrap logger (catches startup errors) ───────────────────────────────
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting OrderService...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ─── Serilog ─────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .Enrich.WithProperty("Service", "OrderService")
           .WriteTo.Console());

    // ─── AWS + DynamoDB ───────────────────────────────────────────────────────
    var awsOptions = builder.Configuration.GetAWSOptions();
    builder.Services.AddDefaultAWSOptions(awsOptions);
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
    builder.Services.AddScoped<IEventHandler<OrderPlacedEvent>, OrderPlacedEventHandler>();
    builder.Services.AddScoped<IEventHandler<OrderCancelledEvent>, OrderCancelledEventHandler>();

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
    builder.Services.AddMediatR(cfg =>
        cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

    // Validation pipeline — runs before every handler
    builder.Services.AddTransient(
        typeof(IPipelineBehavior<,>),
        typeof(ValidationBehaviour<,>));

    // FluentValidation — scans assembly for all validators
    builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

    // ─── Health Checks ────────────────────────────────────────────────────────
    builder.Services.AddHealthChecks();

    var app = builder.Build();

    // ─── Middleware Pipeline ──────────────────────────────────────────────────
    app.UseSerilogRequestLogging(opts =>
    {
        opts.MessageTemplate =
            "HTTP {RequestMethod} {RequestPath} → {StatusCode} ({Elapsed:0.0000}ms)";
    });

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
    app.MapHealthChecks("/health");

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