// ========================================
// ARCHIVO: src/SistemaOpinionesETL.Presentation/Worker.cs
// ========================================
using Microsoft.Extensions.Options;
using SistemaOpinionesETL.Core.Common;
using SistemaOpinionesETL.Core.Interfaces;

namespace SistemaOpinionesETL.Presentation;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IServiceProvider _serviceProvider;
    private readonly ConfiguracionExtraccion _configuracion;

    public Worker(
        ILogger<Worker> logger,
        IServiceProvider serviceProvider,
        IOptions<ConfiguracionExtraccion> configuracion)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _configuracion = configuracion.Value ?? throw new ArgumentNullException(nameof(configuracion));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("🚀 Worker Service ETL iniciado en: {Tiempo}", DateTimeOffset.Now);
        _logger.LogInformation("📊 Configuración: Directorio staging = {Dir}",
            _configuracion.Almacenamiento.DirectorioStaging);

        // Esperar 5 segundos antes de la primera ejecución
        await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            var inicioCiclo = DateTime.Now;
            _logger.LogInformation("🔄 Iniciando ciclo de extracción ETL - {Hora}",
                inicioCiclo.ToString("HH:mm:ss"));

            try
            {
                // Crear scope para resolver servicios scoped
                using var alcance = _serviceProvider.CreateScope();
                var orquestador = alcance.ServiceProvider
                    .GetRequiredService<IOrquestadorExtraccion>();

                // Ejecutar extracción
                var resumen = await orquestador.EjecutarExtraccionAsync();

                _logger.LogInformation(
                    "✅ ETL completado. Fuentes exitosas: {Exitosas}/{Total}, Registros: {Registros}, Tiempo: {Tiempo}",
                    resumen.FuentesExitosas, resumen.TotalFuentes,
                    resumen.TotalRegistrosExtraidos, resumen.TiempoTotal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Error en el proceso ETL");
            }

            // Calcular tiempo de espera para el próximo ciclo
            var tiempoEjecucion = DateTime.Now - inicioCiclo;
            var tiempoEspera = TimeSpan.FromMinutes(5) - tiempoEjecucion;

            if (tiempoEspera > TimeSpan.Zero)
            {
                _logger.LogInformation("⏳ Próxima ejecución en {Tiempo:hh\\:mm\\:ss}", tiempoEspera);
                await Task.Delay(tiempoEspera, stoppingToken);
            }
            else
            {
                _logger.LogWarning("⚠️ El ciclo ETL tardó más de 5 minutos. Continuando inmediatamente.");
            }
        }

        _logger.LogInformation("🛑 Worker Service ETL finalizado");
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("▶️  Iniciando Worker Service ETL...");
        await base.StartAsync(cancellationToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("⏹️  Deteniendo Worker Service ETL...");
        await base.StopAsync(cancellationToken);
    }
}