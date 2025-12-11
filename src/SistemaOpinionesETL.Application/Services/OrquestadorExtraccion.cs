
using System.Diagnostics;
using SistemaOpinionesETL.Core.Common;
using SistemaOpinionesETL.Core.Entities;
using SistemaOpinionesETL.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace SistemaOpinionesETL.Application.Services;

public class OrquestadorExtraccion : IOrquestadorExtraccion
{
    private readonly IEnumerable<IExtractor> _extractores;
    private readonly IDataStagingService _stagingService;
    private readonly ILogger<OrquestadorExtraccion> _logger;

    public OrquestadorExtraccion(
        IEnumerable<IExtractor> extractores,
        IDataStagingService stagingService,
        ILogger<OrquestadorExtraccion> logger)
    {
        _extractores = extractores ?? throw new ArgumentNullException(nameof(extractores));
        _stagingService = stagingService ?? throw new ArgumentNullException(nameof(stagingService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ResumenExtraccion> EjecutarExtraccionAsync()
    {
        var stopwatchTotal = Stopwatch.StartNew();
        var resumen = new ResumenExtraccion();

        _logger.LogInformation("🚀 Iniciando proceso de extracción ETL");

        try
        {
            // Ejecutar extractores en paralelo para mejor rendimiento
            var tareasExtraccion = _extractores.Select(extractor =>
                EjecutarExtractorAsync(extractor));

            var resultados = await Task.WhenAll(tareasExtraccion);
            resumen.Resultados.AddRange(resultados);

            // Procesar resultados
            foreach (var resultado in resultados)
            {
                resumen.TotalFuentes++;

                if (resultado.Exitoso)
                {
                    resumen.FuentesExitosas++;
                    resumen.TotalRegistrosExtraidos += resultado.RegistrosExtraidos;

                    // Guardar en staging si hay datos
                    if (resultado.Comentarios.Any())
                    {
                        await _stagingService.GuardarEnAlmacenamientoAsync(resultado.Comentarios);
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ Extracción fallida: {Fuente} - {Error}",
                        resultado.NombreFuente, resultado.MensajeError);
                }
            }

            stopwatchTotal.Stop();
            resumen.TiempoTotal = stopwatchTotal.Elapsed;

            _logger.LogInformation(
                "✅ Extracción completada: {Exitosas}/{Total} fuentes, {Registros} registros, {Tiempo}ms",
                resumen.FuentesExitosas, resumen.TotalFuentes,
                resumen.TotalRegistrosExtraidos, stopwatchTotal.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error crítico en el orquestador de extracción");
        }

        return resumen;
    }

    private async Task<ResultadoExtraccion> EjecutarExtractorAsync(IExtractor extractor)
    {
        try
        {
            _logger.LogDebug("Ejecutando extractor: {Nombre}", extractor.Nombre);
            return await extractor.ExtraerAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en extractor {Nombre}", extractor.Nombre);
            return new ResultadoExtraccion
            {
                NombreFuente = extractor.Nombre,
                Exitoso = false,
                MensajeError = ex.Message
            };
        }
    }
}