// ========================================
// ARCHIVO: src/SistemaOpinionesETL.Infrastructure/Extractors/CsvExtractor.cs
// ========================================
using CsvHelper;
using CsvHelper.Configuration;
using System.Diagnostics;
using System.Globalization;
using SistemaOpinionesETL.Core.Common;
using SistemaOpinionesETL.Core.Entities;
using SistemaOpinionesETL.Core.Interfaces;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace SistemaOpinionesETL.Infrastructure.Extractors;

public class CsvExtractor : IExtractor
{
    private readonly string _rutaArchivo;
    private readonly ILogger<CsvExtractor> _logger;

    public TipoFuente TipoFuente => TipoFuente.CSV;
    public string Nombre => "CSV Extractor";

    public CsvExtractor(string rutaArchivo, ILogger<CsvExtractor> logger)
    {
        _rutaArchivo = rutaArchivo ?? throw new ArgumentNullException(nameof(rutaArchivo));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool ValidarConfiguracion()
    {
        if (string.IsNullOrWhiteSpace(_rutaArchivo))
        {
            _logger.LogError("Ruta de archivo CSV no configurada");
            return false;
        }

        if (!File.Exists(_rutaArchivo))
        {
            _logger.LogWarning("Archivo CSV no encontrado: {Ruta}", _rutaArchivo);
            return false;
        }

        return true;
    }

    public async Task<ResultadoExtraccion> ExtraerAsync(CancellationToken cancellationToken = default)
    {
        var stopwatch = Stopwatch.StartNew();
        var resultado = new ResultadoExtraccion
        {
            NombreFuente = Nombre,
            FechaInicio = DateTime.UtcNow
        };

        try
        {
            _logger.LogInformation("Iniciando extracción desde CSV: {Ruta}", _rutaArchivo);

            if (!ValidarConfiguracion())
            {
                resultado.Exitoso = false;
                resultado.MensajeError = "Configuración inválida o archivo no encontrado";
                return resultado;
            }

            using var reader = new StreamReader(_rutaArchivo);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = ",",
                HasHeaderRecord = true,
                MissingFieldFound = null
            };

            using var csv = new CsvReader(reader, config);
            csv.Context.RegisterClassMap<ComentarioMap>();

            var registros = await csv.GetRecordsAsync<Comentario>()
                .ToListAsync(cancellationToken);

            resultado.Comentarios = registros;
            resultado.Exitoso = true;

            _logger.LogInformation("✅ CSV: {Registros} registros extraídos en {Tiempo}ms",
                registros.Count, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            resultado.Exitoso = false;
            resultado.MensajeError = ex.Message;
            _logger.LogError(ex, "❌ Error extrayendo datos de CSV");
        }
        finally
        {
            stopwatch.Stop();
            resultado.TiempoExtraccion = stopwatch.Elapsed;
            resultado.FechaFin = DateTime.UtcNow;
        }

        return resultado;
    }
}

public class ComentarioMap : ClassMap<Comentario>
{
    public ComentarioMap()
    {
        Map(m => m.ProductoId).Name("ProductId", "IdProducto");
        Map(m => m.ClienteId).Name("CustomerId", "IdCliente").Optional();
        Map(m => m.FechaCreacion).Name("CreatedAt", "Fecha");
        Map(m => m.Texto).Name("Text", "Comentario", "CommentText");
        Map(m => m.Calificacion).Name("Rating", "Calificacion").Optional();
        Map(m => m.Fuente).Name("Source", "Fuente");
        Map(m => m.TipoFuente).Convert(_ => TipoFuente.CSV);
        Map(m => m.FechaImportacion).Convert(_ => DateTime.UtcNow);
    }
}