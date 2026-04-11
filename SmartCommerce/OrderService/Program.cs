using Amazon.DynamoDBv2;
using Amazon.Extensions.NETCore.Setup;
using OrderService.Application.Interfaces;
using OrderService.Infrastructure.DynamoDB;
using Serilog;

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

    // ─── App Services ─────────────────────────────────────────────────────────
    builder.Services.AddControllers();
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