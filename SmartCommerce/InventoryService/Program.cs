using Amazon.SQS;
using InventoryService.Handlers;
using InventoryService.Infrastructure;
using InventoryService.Workers;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting InventoryService...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .Enrich.WithProperty("Service", "InventoryService"));

    // SQS
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

    // Handler + Worker
    builder.Services.AddSingleton<InventoryReservationHandler>();
    builder.Services.AddHostedService<InventoryConsumerWorker>();

    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.MapHealthChecks("/health");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "InventoryService failed to start.");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;