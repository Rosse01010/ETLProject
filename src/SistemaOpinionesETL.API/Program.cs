
using Serilog;
using SistemaOpinionesETL.Core.Common;
using SistemaOpinionesETL.Core.Interfaces;
using SistemaOpinionesETL.Application.Services;
using SistemaOpinionesETL.Infrastructure.Extractors;
using SistemaOpinionesETL.Infrastructure.Services;

var builder = WebApplication.CreateBuilder(args);

// Configurar Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configuración desde appsettings.json
builder.Services.Configure<ConfiguracionExtraccion>(
    builder.Configuration.GetSection("ConfiguracionExtraccion"));

var config = builder.Configuration
    .GetSection("ConfiguracionExtraccion")
    .Get<ConfiguracionExtraccion>() ?? new ConfiguracionExtraccion();

// Registrar HttpClient
builder.Services.AddHttpClient();

// Registrar Extractores
builder.Services.AddTransient<IExtractor>(sp =>
    new MultiCsvExtractor(
        new List<string> { "Data/surveys_part1.csv" },  
        sp.GetRequiredService<ILogger<MultiCsvExtractor>>()));

builder.Services.AddTransient<IExtractor>(sp =>
    new DatabaseExtractor(
        config.BaseDatos.ConnectionString,
        sp.GetRequiredService<ILogger<DatabaseExtractor>>()));

builder.Services.AddTransient<IExtractor>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient();
    var url = $"{config.API.UrlBase}/{config.API.Endpoint}";
    return new ApiExtractor(url, httpClient, sp.GetRequiredService<ILogger<ApiExtractor>>());
});

// Registrar Servicios
builder.Services.AddSingleton<IDataStagingService>(sp =>
    new StagingService(
        config.Almacenamiento.DirectorioStaging,
        sp.GetRequiredService<ILogger<StagingService>>()));

builder.Services.AddScoped<IOrquestadorExtraccion, OrquestadorExtraccion>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

Log.Information("🚀 API ETL iniciada en: {Url}", "https://localhost:7001");

app.Run();