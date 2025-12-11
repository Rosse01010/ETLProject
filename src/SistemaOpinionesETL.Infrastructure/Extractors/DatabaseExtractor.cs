
using Dapper;
using Microsoft.Data.SqlClient;
using System.Diagnostics;
using SistemaOpinionesETL.Core.Common;
using SistemaOpinionesETL.Core.Entities;
using SistemaOpinionesETL.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace SistemaOpinionesETL.Infrastructure.Extractors;

public class DatabaseExtractor : IExtractor
{
    private readonly string _cadenaConexion;
    private readonly ILogger<DatabaseExtractor> _logger;

    public TipoFuente TipoFuente => TipoFuente.BaseDeDatos;
    public string Nombre => "Database Extractor";

    public DatabaseExtractor(string cadenaConexion, ILogger<DatabaseExtractor> logger)
    {
        _cadenaConexion = cadenaConexion ?? throw new ArgumentNullException(nameof(cadenaConexion));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool ValidarConfiguracion()
    {
        if (string.IsNullOrWhiteSpace(_cadenaConexion))
        {
            _logger.LogError("Connection string no configurada");
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
            _logger.LogInformation("Iniciando extracción desde base de datos");

            if (!ValidarConfiguracion())
            {
                resultado.Exitoso = false;
                resultado.MensajeError = "Configuración inválida";
                return resultado;
            }

            using var conexion = new SqlConnection(_cadenaConexion);
            await conexion.OpenAsync(cancellationToken);

            var query = @"
    SELECT 
        dp.product_id as ProductoId,
        dc.client_id as ClienteId,
        dt.full_date as FechaCreacion,
        fo.comment_text as TextoComentario,
        CAST(fo.rating AS INT) as Calificacion,
        'ReseñasWeb' as Fuente
    FROM fact_opinions fo
    JOIN dim_product dp ON fo.product_key = dp.product_key
    JOIN dim_client dc ON fo.client_key = dc.client_key
    JOIN dim_time dt ON fo.time_key = dt.time_key
    WHERE dt.full_date >= DATEADD(day, -1, GETDATE())";

            var registros = (await conexion.QueryAsync<Comentario>(query)).ToList();

            // Establecer TipoFuente manualmente
            foreach (var comentario in registros)
            {
                comentario.TipoFuente = TipoFuente.BaseDeDatos;
                comentario.FechaImportacion = DateTime.UtcNow;
            }

            resultado.Comentarios = registros;
            resultado.Exitoso = true;

            _logger.LogInformation("✅ Base de Datos: {Registros} registros extraídos en {Tiempo}ms",
                registros.Count, stopwatch.ElapsedMilliseconds);
        }
        catch (SqlException ex)
        {
            resultado.Exitoso = false;
            resultado.MensajeError = $"Error de base de datos: {ex.Message}";
            _logger.LogError(ex, "❌ Error SQL en extracción de BD");
        }
        catch (Exception ex)
        {
            resultado.Exitoso = false;
            resultado.MensajeError = ex.Message;
            _logger.LogError(ex, "❌ Error extrayendo datos de base de datos");
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