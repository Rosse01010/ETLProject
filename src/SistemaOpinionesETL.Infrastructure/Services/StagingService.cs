// ========================================
// ARCHIVO: src/SistemaOpinionesETL.Infrastructure/Services/StagingService.cs
// ========================================
using System.Text.Json;
using SistemaOpinionesETL.Core.Entities;
using SistemaOpinionesETL.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace SistemaOpinionesETL.Infrastructure.Services;

public class StagingService : IDataStagingService
{
    private readonly string _directorioStaging;
    private readonly ILogger<StagingService> _logger;

    public StagingService(string directorioStaging, ILogger<StagingService> logger)
    {
        _directorioStaging = directorioStaging ?? throw new ArgumentNullException(nameof(directorioStaging));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Directory.CreateDirectory(_directorioStaging);
        Directory.CreateDirectory(Path.Combine(_directorioStaging, "procesados"));
    }

    public async Task<string?> GuardarEnAlmacenamientoAsync(IEnumerable<Comentario> comentarios)
    {
        if (!comentarios.Any())
        {
            _logger.LogWarning("No hay comentarios para guardar en staging");
            return null;
        }

        try
        {
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var nombreArchivo = $"staging_{timestamp}.json";
            var rutaArchivo = Path.Combine(_directorioStaging, nombreArchivo);

            var opciones = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            var json = JsonSerializer.Serialize(comentarios, opciones);
            await File.WriteAllTextAsync(rutaArchivo, json);

            _logger.LogInformation("✅ Guardados {Count} comentarios en staging: {FilePath}",
                comentarios.Count(), rutaArchivo);

            return rutaArchivo;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error guardando datos en staging");
            return null;
        }
    }

    public Task<int> ObtenerCantidadAlmacenadaAsync()
    {
        var archivos = Directory.GetFiles(_directorioStaging, "staging_*.json");
        return Task.FromResult(archivos.Length);
    }

    public IEnumerable<string> ObtenerArchivosStaging()
    {
        return Directory.GetFiles(_directorioStaging, "staging_*.json")
                       .OrderBy(f => f);
    }

    public Task LimpiarArchivosViejosAsync(int diasRetencion = 7)
    {
        try
        {
            var fechaLimite = DateTime.Now.AddDays(-diasRetencion);
            var archivos = Directory.GetFiles(_directorioStaging, "staging_*.json");

            foreach (var archivo in archivos)
            {
                var infoArchivo = new FileInfo(archivo);
                if (infoArchivo.CreationTime < fechaLimite)
                {
                    File.Delete(archivo);
                    _logger.LogInformation("Eliminado archivo viejo: {Archivo}", archivo);
                }
            }

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "❌ Error limpiando archivos viejos");
            return Task.CompletedTask;
        }
    }
}