// ========================================
// ARCHIVO: src/SistemaOpinionesETL.Presentation/Program.cs
// ========================================
using Serilog;
using SistemaOpinionesETL.Core.Common;
using SistemaOpinionesETL.Core.Interfaces;
using SistemaOpinionesETL.Application.Services;
using SistemaOpinionesETL.Infrastructure.Extractors;
using SistemaOpinionesETL.Infrastructure.Services;
using SistemaOpinionesETL.Presentation;

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(
        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .WriteTo.File(
        "logs/etl-worker-.log",
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

try
{
    Log.Information("🚀 Iniciando aplicación Worker ETL");

    var builder = Host.CreateApplicationBuilder(args);

    // Configurar Serilog
    builder.Services.AddSerilog();

    // Configuración desde appsettings.json
    builder.Services.Configure<ConfiguracionExtraccion>(
        builder.Configuration.GetSection("ConfiguracionExtraccion"));

    var config = builder.Configuration
        .GetSection("ConfiguracionExtraccion")
        .Get<ConfiguracionExtraccion>() ?? new ConfiguracionExtraccion();

    // Configuración de Connection String para BD Analítica
    var connectionStringAnalytica = builder.Configuration
        .GetConnectionString("AnalyticsDB")
        ?? "Server=localhost;Database=OpinionAnalytics;Trusted_Connection=true;TrustServerCertificate=true;";

    // Registrar DataLoader
    builder.Services.AddSingleton<IDataLoader>(sp =>
        new DataLoader(
            connectionStringAnalytica,
            sp.GetRequiredService<ILogger<DataLoader>>()));

    // Registrar HttpClient
    builder.Services.AddHttpClient();

    // Registrar Extractores
    builder.Services.AddTransient<IExtractor>(sp =>
        new CsvExtractor(
            config.CSV.RutaArchivo,
            sp.GetRequiredService<ILogger<CsvExtractor>>()));

    builder.Services.AddTransient<IExtractor>(sp =>
        new DatabaseExtractor(
            config.BaseDatos.ConnectionString,
            sp.GetRequiredService<ILogger<DatabaseExtractor>>()));

    builder.Services.AddTransient<IExtractor>(sp =>
    {
        var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
        var httpClient = httpClientFactory.CreateClient();
        var url = $"{config.API.UrlBase}/{config.API.Endpoint}";
        return new ApiExtractor(
            url,
            httpClient,
            sp.GetRequiredService<ILogger<ApiExtractor>>());
    });

    // Registrar Servicios
    builder.Services.AddSingleton<IDataStagingService>(sp =>
        new StagingService(
            config.Almacenamiento.DirectorioStaging,
            sp.GetRequiredService<ILogger<StagingService>>()));

    builder.Services.AddScoped<IOrquestadorExtraccion, OrquestadorExtraccion>();

    // Registrar Worker
    builder.Services.AddHostedService<Worker>();

    var host = builder.Build();

    Log.Information("✅ Aplicación configurada correctamente");

    await host.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "❌ Error fatal en la aplicación");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;