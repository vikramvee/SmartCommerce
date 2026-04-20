using Amazon.SQS;
using NotificationService.Handlers;
using NotificationService.Infrastructure;
using NotificationService.Workers;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting NotificationService...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Serilog
    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .Enrich.WithProperty("Service", "NotificationService"));

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

    // Handlers + Worker
    builder.Services.AddSingleton<OrderNotificationHandler>();
    builder.Services.AddHostedService<NotificationConsumerWorker>();

    builder.Services.AddHealthChecks();

    var app = builder.Build();

    app.UseSerilogRequestLogging();
    app.MapHealthChecks("/health");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "NotificationService failed to start.");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;