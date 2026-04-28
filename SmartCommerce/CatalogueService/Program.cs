using Amazon.DynamoDBv2;
using CatalogueService.Infrastructure.AI;
using CatalogueService.Infrastructure.DynamoDB;
using MediatR;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting CatalogueService...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // ─── Serilog ─────────────────────────────────────────────────────────────
    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .Enrich.WithProperty("Service", "CatalogueService"));

    // ─── DynamoDB ─────────────────────────────────────────────────────────────
    builder.Services.AddSingleton<IAmazonDynamoDB>(_ =>
    {
        var serviceUrl = builder.Configuration["DynamoDB:ServiceURL"];
        var config     = new AmazonDynamoDBConfig
        {
            RegionEndpoint = Amazon.RegionEndpoint.USEast1
        };
        if (!string.IsNullOrEmpty(serviceUrl))
            config.ServiceURL = serviceUrl;

        return new AmazonDynamoDBClient("local", "local", config);
    });
    builder.Services.AddScoped<IProductRepository, DynamoDbProductRepository>();

    // ─── Embeddings ───────────────────────────────────────────────────────────
    var useStub = builder.Configuration.GetValue<bool>("Bedrock:UseStub");
    if (useStub)
        builder.Services.AddSingleton<IEmbeddingService, StubEmbeddingService>();
    else
        builder.Services.AddSingleton<IEmbeddingService, BedrockEmbeddingService>();

    // ─── MediatR ──────────────────────────────────────────────────────────────
    builder.Services.AddMediatR(cfg => 
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new()
        {
            Title       = "SmartCommerce - Catalogue Service",
            Version     = "v1",
            Description = "AI-powered product catalogue with semantic search"
        });
    });

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "Catalogue Service v1");
            c.RoutePrefix = string.Empty;
        });
    }

    app.UseAuthorization();
    app.MapControllers();
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "CatalogueService failed to start");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}

return 0;

public partial class Program { }