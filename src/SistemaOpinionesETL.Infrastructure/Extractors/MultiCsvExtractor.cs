using Microsoft.Extensions.Logging;
using SistemaOpinionesETL.Core.Common;
using SistemaOpinionesETL.Core.Entities;
using SistemaOpinionesETL.Core.Interfaces;
using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Diagnostics;

namespace SistemaOpinionesETL.Infrastructure.Extractors;



public class MultiCsvExtractor : IExtractor
{
    private readonly List<string> _rutasArchivos;
    private readonly ILogger<MultiCsvExtractor> _logger;

    public TipoFuente TipoFuente => TipoFuente.CSV;
    public string Nombre => "Multi CSV Extractor";

    public MultiCsvExtractor(List<string> rutasArchivos, ILogger<MultiCsvExtractor> logger)
    {
        _rutasArchivos = rutasArchivos ?? throw new ArgumentNullException(nameof(rutasArchivos));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool ValidarConfiguracion()
    {
        if (_rutasArchivos.Count == 0)
        {
            _logger.LogError("No se han configurado rutas de archivos CSV");
            return false;
        }

        var archivosExistentes = _rutasArchivos.Count(ruta => File.Exists(ruta));
        if (archivosExistentes == 0)
        {
            _logger.LogWarning("Ninguno de los archivos CSV configurados existe");
            return false;
        }

        return true;
    }

    // Mapeo para archivos de encuestas/surveys
    public class SurveyCsvMap : ClassMap<Comentario>
    {
        public SurveyCsvMap()
        {
            Map(m => m.ProductoId).Name("ProductId", "product_id", "IdProducto", "ProductID");
            Map(m => m.ClienteId).Name("CustomerId", "client_id", "IdCliente", "CustomerID");
            Map(m => m.FechaCreacion).Name("CreatedAt", "fecha", "Fecha", "Date", "Timestamp");
            Map(m => m.Texto).Name("Text", "comentario", "comment_text", "Comment", "Review");
            Map(m => m.Calificacion).Name("Rating", "calificacion", "puntaje", "Score").Optional();
            Map(m => m.Fuente).Name("Source", "fuente", "Origen").Default("Survey CSV");
            Map(m => m.TipoFuente).Convert(_ => TipoFuente.CSV);
            Map(m => m.FechaImportacion).Convert(_ => DateTime.UtcNow);
        }
    }

    // Mapeo para archivos de clientes
    public class ClientCsvMap : ClassMap<Comentario>
    {
        public ClientCsvMap()
        {
            Map(m => m.ClienteId).Name("ClientId", "CustomerId", "IdCliente", "CustomerID");
            Map(m => m.Texto).Name("Comments", "Observaciones", "Notas").Optional();
            Map(m => m.Fuente).Name("Source", "fuente").Default("Clients CSV");
            Map(m => m.TipoFuente).Convert(_ => TipoFuente.CSV);
            Map(m => m.FechaCreacion).Convert(_ => DateTime.UtcNow);
            Map(m => m.FechaImportacion).Convert(_ => DateTime.UtcNow);
        }
    }

    // Mapeo para archivos de productos
    public class ProductCsvMap : ClassMap<Comentario>
    {
        public ProductCsvMap()
        {
            Map(m => m.ProductoId).Name("ProductId", "IdProducto", "ProductID");
            Map(m => m.Texto).Name("Description", "Descripcion", "Comments").Optional();
            Map(m => m.Calificacion).Name("Rating", "Calificacion").Optional();
            Map(m => m.Fuente).Name("Source", "fuente").Default("Products CSV");
            Map(m => m.TipoFuente).Convert(_ => TipoFuente.CSV);
            Map(m => m.FechaCreacion).Convert(_ => DateTime.UtcNow);
            Map(m => m.FechaImportacion).Convert(_ => DateTime.UtcNow);
        }
    }

    // Mapeo para archivos de fuente de datos
    public class FuenteDatosCsvMap : ClassMap<Comentario>
    {
        public FuenteDatosCsvMap()
        {
            Map(m => m.ProductoId).Name("product_id", "ProductId").Optional();
            Map(m => m.ClienteId).Name("client_id", "CustomerId").Optional();
            Map(m => m.Texto).Name("comment", "texto", "observacion").Optional();
            Map(m => m.Calificacion).Name("rating", "score").Optional();
            Map(m => m.Fuente).Name("source", "fuente").Default("Fuente Datos CSV");
            Map(m => m.TipoFuente).Convert(_ => TipoFuente.CSV);
            Map(m => m.FechaCreacion).Name("fecha", "date").Optional();
            Map(m => m.FechaImportacion).Convert(_ => DateTime.UtcNow);
        }
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
            _logger.LogInformation("Iniciando extracción desde múltiples archivos CSV");

            if (!ValidarConfiguracion())
            {
                resultado.Exitoso = false;
                resultado.MensajeError = "Configuración inválida o archivos no encontrados";
                return resultado;
            }

            var todosComentarios = new List<Comentario>();

            foreach (var rutaArchivo in _rutasArchivos)
            {
                if (!File.Exists(rutaArchivo))
                {
                    _logger.LogWarning("Archivo CSV no encontrado: {Ruta}", rutaArchivo);
                    continue;
                }

                try
                {
                    var comentarios = await LeerArchivoCsvAsync(rutaArchivo, cancellationToken);
                    todosComentarios.AddRange(comentarios);

                    _logger.LogInformation("✅ Archivo {Archivo}: {Registros} registros extraídos",
                        Path.GetFileName(rutaArchivo), comentarios.Count);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "❌ Error leyendo archivo CSV: {Ruta}", rutaArchivo);
                }
            }

            resultado.Comentarios = todosComentarios;
            resultado.Exitoso = true;

            _logger.LogInformation("✅ Multi CSV: {Archivos} archivos, {Registros} registros extraídos en {Tiempo}ms",
                _rutasArchivos.Count, todosComentarios.Count, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            resultado.Exitoso = false;
            resultado.MensajeError = ex.Message;
            _logger.LogError(ex, "❌ Error extrayendo datos de múltiples CSV");
        }
        finally
        {
            stopwatch.Stop();
            resultado.TiempoExtraccion = stopwatch.Elapsed;
            resultado.FechaFin = DateTime.UtcNow;
        }

        return resultado;
    }

    private async Task<List<Comentario>> LeerArchivoCsvAsync(string rutaArchivo, CancellationToken cancellationToken)
    {
        var comentarios = new List<Comentario>();

        // Determinar el tipo de archivo por su nombre para aplicar el mapeo correcto
        var nombreArchivo = Path.GetFileName(rutaArchivo).ToLower();

        using var reader = new StreamReader(rutaArchivo);
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    Delimiter = ",",
    HasHeaderRecord = true,
    MissingFieldFound = null,
    BadDataFound = null,
    HeaderValidated = null  // Desactivar validación de encabezados
};;

        using var csv = new CsvReader(reader, config);

        // Aplicar mapeo basado en el tipo de archivo
        if (nombreArchivo.Contains("survey") || nombreArchivo.Contains("encuesta"))
        {
            csv.Context.RegisterClassMap<SurveyCsvMap>();
        }
        else if (nombreArchivo.Contains("client"))
        {
            csv.Context.RegisterClassMap<ClientCsvMap>();
        }
        else if (nombreArchivo.Contains("product"))
        {
            csv.Context.RegisterClassMap<ProductCsvMap>();
        }
        else if (nombreArchivo.Contains("fuente"))
        {
            csv.Context.RegisterClassMap<FuenteDatosCsvMap>();
        }
        else
        {
            csv.Context.RegisterClassMap<ComentarioMap>(); // Mapeo por defecto
        }

        var registros = await csv.GetRecordsAsync<Comentario>().ToListAsync(cancellationToken);
        return registros;
    }
}
