using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;
using SistemaOpinionesETL.Core.Entities;
using SistemaOpinionesETL.Core.Entities.ModeloEstrella;
using SistemaOpinionesETL.Core.Interfaces;
using SistemaOpinionesETL.Core.Common;
using Microsoft.Extensions.Logging;

namespace SistemaOpinionesETL.Infrastructure.Services;


public class DataLoader : IDataLoader
{
    private readonly string _connectionString;
    private readonly ILogger<DataLoader> _logger;

    // Cachés para claves de dimensiones
    private readonly Dictionary<string, int> _productKeyCache = new();
    private readonly Dictionary<string, int> _clientKeyCache = new();
    private readonly Dictionary<int, int> _timeKeyCache = new();
    private readonly Dictionary<string, int> _sourceKeyCache = new();
    private readonly Dictionary<int, int> _sentimentKeyCache = new();

    public DataLoader(string connectionString, ILogger<DataLoader> logger)
    {
        _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<bool> ValidarConexionAsync()
    {
        try
        {
            using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();
            _logger.LogInformation("✅ Conexión a BD analítica exitosa");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Error validando conexión a BD analítica");
            return false;
        }
    }

    public async Task<int> CargarABaseAnaliticaAsync(IEnumerable<Comentario> comentarios)
    {
        if (!comentarios.Any())
        {
            _logger.LogWarning("No hay comentarios para cargar");
            return 0;
        }

        try
        {
            _logger.LogInformation(" Iniciando carga de {Count} comentarios a BD analítica", comentarios.Count());

            using var conexion = new SqlConnection(_connectionString);
            await conexion.OpenAsync();

            using var transaccion = conexion.BeginTransaction();

            try
            {
                // 1. Cargar dimensiones
                _logger.LogDebug(" Cargando dimensiones...");
                await CargarDimensionesAsync(conexion, transaccion, comentarios);

                // 2. Cargar tabla de hechos
                _logger.LogDebug("Cargando fact_opinions...");
                var registrosCargados = await CargarFactOpinionsAsync(conexion, transaccion, comentarios);

                await transaccion.CommitAsync();

                _logger.LogInformation(" Cargados {Registros} registros a fact_opinions", registrosCargados);
                return registrosCargados;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, " Error en transacción, rollback");
                await transaccion.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Error en carga masiva a BD analítica");
            throw;
        }
    }

    public async Task<int> CargarDesdeStagingAsync(string rutaArchivo)
    {
        if (!File.Exists(rutaArchivo))
        {
            _logger.LogError(" Archivo de staging no encontrado: {Ruta}", rutaArchivo);
            return 0;
        }

        try
        {
            _logger.LogInformation(" Leyendo archivo: {Archivo}", Path.GetFileName(rutaArchivo));

            var json = await File.ReadAllTextAsync(rutaArchivo);
            var comentarios = JsonSerializer.Deserialize<IEnumerable<Comentario>>(json);

            if (comentarios == null || !comentarios.Any())
            {
                _logger.LogWarning("  Archivo de staging vacío");
                return 0;
            }

            var registrosCargados = await CargarABaseAnaliticaAsync(comentarios);

            // Mover archivo a carpeta de procesados
            var directorioProcesados = Path.Combine(Path.GetDirectoryName(rutaArchivo)!, "procesados");
            Directory.CreateDirectory(directorioProcesados);

            var nombreArchivo = Path.GetFileName(rutaArchivo);
            var rutaDestino = Path.Combine(directorioProcesados, nombreArchivo);

            if (File.Exists(rutaDestino))
            {
                File.Delete(rutaDestino);
            }

            File.Move(rutaArchivo, rutaDestino);
            _logger.LogInformation(" Archivo movido a procesados: {Archivo}", nombreArchivo);

            return registrosCargados;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, " Error cargando desde staging");
            return 0;
        }
    }

    
    private async Task CargarDimensionesAsync(
        SqlConnection conexion,
        SqlTransaction transaccion,
        IEnumerable<Comentario> comentarios)
    {
        await CargarDimSentimentAsync(conexion, transaccion);
        await CargarDimSourceAsync(conexion, transaccion, comentarios);
        await CargarDimProductAsync(conexion, transaccion, comentarios);
        await CargarDimClientAsync(conexion, transaccion, comentarios);
        await CargarDimTimeAsync(conexion, transaccion, comentarios);

        _logger.LogDebug("✅ Dimensiones cargadas");
    }

    
    private async Task CargarDimSentimentAsync(SqlConnection conexion, SqlTransaction transaccion)
    {
        var sentimientos = Enum.GetValues<Sentimiento>();

        foreach (var sentimiento in sentimientos)
        {
            var sentimentoId = ((int)sentimiento).ToString();

            if (_sentimentKeyCache.ContainsKey((int)sentimiento))
                continue;

            var query = @"
                IF NOT EXISTS (SELECT 1 FROM dim_sentiment WHERE sentiment_id = @sentiment_id)
                BEGIN
                    INSERT INTO dim_sentiment (
                        sentiment_id, sentiment_name, sentiment_score, 
                        score_range_min, score_range_max, color_code, created_date, is_active
                    )
                    VALUES (
                        @sentiment_id, @sentiment_name, @sentiment_score,
                        @score_min, @score_max, @color_code, GETUTCDATE(), 1
                    )
                END
                
                SELECT sentiment_key FROM dim_sentiment WHERE sentiment_id = @sentiment_id";

            using var cmd = new SqlCommand(query, conexion, transaccion);
            cmd.Parameters.AddWithValue("@sentiment_id", sentimentoId);
            cmd.Parameters.AddWithValue("@sentiment_name", ObtenerNombreSentimiento(sentimiento));
            cmd.Parameters.AddWithValue("@sentiment_score", (int)sentimiento);

            var (min, max) = ObtenerRangoSentimiento(sentimiento);
            cmd.Parameters.AddWithValue("@score_min", min);
            cmd.Parameters.AddWithValue("@score_max", max);
            cmd.Parameters.AddWithValue("@color_code", ObtenerColorSentimiento(sentimiento));

            var sentimentKey = await cmd.ExecuteScalarAsync();
            if (sentimentKey != null)
            {
                _sentimentKeyCache[(int)sentimiento] = Convert.ToInt32(sentimentKey);
                _logger.LogDebug("  ✓ Sentimiento: {Nombre}", ObtenerNombreSentimiento(sentimiento));
            }
        }
    }

    
    private async Task CargarDimSourceAsync(
        SqlConnection conexion,
        SqlTransaction transaccion,
        IEnumerable<Comentario> comentarios)
    {
        var fuentesUnicas = comentarios
            .Select(c => new { c.Fuente, c.TipoFuente })
            .Distinct()
            .ToList();

        foreach (var fuente in fuentesUnicas)
        {
            if (_sourceKeyCache.ContainsKey(fuente.Fuente))
                continue;

            var query = @"
                IF NOT EXISTS (SELECT 1 FROM dim_source WHERE source_id = @source_id)
                BEGIN
                    INSERT INTO dim_source (
                        source_id, source_name, source_type, source_category,
                        reliability_score, is_active, created_date
                    )
                    VALUES (
                        @source_id, @source_name, @source_type, @source_category,
                        @reliability_score, 1, GETUTCDATE()
                    )
                END
                
                SELECT source_key FROM dim_source WHERE source_id = @source_id";

            using var cmd = new SqlCommand(query, conexion, transaccion);
            cmd.Parameters.AddWithValue("@source_id", fuente.Fuente);
            cmd.Parameters.AddWithValue("@source_name", fuente.Fuente);
            cmd.Parameters.AddWithValue("@source_type", fuente.TipoFuente.ToString());
            cmd.Parameters.AddWithValue("@source_category", ObtenerCategoriaPorTipo(fuente.TipoFuente));
            cmd.Parameters.AddWithValue("@reliability_score", 1.0m);

            var sourceKey = await cmd.ExecuteScalarAsync();
            if (sourceKey != null)
            {
                _sourceKeyCache[fuente.Fuente] = Convert.ToInt32(sourceKey);
                _logger.LogDebug("  ✓ Fuente: {Nombre} ({Tipo})", fuente.Fuente, fuente.TipoFuente);
            }
        }
    }

    
    private async Task CargarDimProductAsync(
        SqlConnection conexion,
        SqlTransaction transaccion,
        IEnumerable<Comentario> comentarios)
    {
        var productosUnicos = comentarios
            .Select(c => c.ProductoId)
            .Distinct()
            .ToList();

        foreach (var productoId in productosUnicos)
        {
            if (_productKeyCache.ContainsKey(productoId))
                continue;

            var query = @"
                IF NOT EXISTS (SELECT 1 FROM dim_product WHERE product_id = @product_id AND is_current = 1)
                BEGIN
                    INSERT INTO dim_product (
                        product_id, product_name, category, subcategory, price,
                        launch_date, is_active, effective_date, is_current, created_date
                    )
                    VALUES (
                        @product_id, @product_name, @category, @subcategory, @price,
                        GETUTCDATE(), 1, GETUTCDATE(), 1, GETUTCDATE()
                    )
                END
                
                SELECT product_key FROM dim_product WHERE product_id = @product_id AND is_current = 1";

            using var cmd = new SqlCommand(query, conexion, transaccion);
            cmd.Parameters.AddWithValue("@product_id", productoId);
            cmd.Parameters.AddWithValue("@product_name", $"Producto {productoId}");
            cmd.Parameters.AddWithValue("@category", "Electrónica"); // Categoría por defecto
            cmd.Parameters.AddWithValue("@subcategory", "General");
            cmd.Parameters.AddWithValue("@price", 0);

            var productKey = await cmd.ExecuteScalarAsync();
            if (productKey != null)
            {
                _productKeyCache[productoId] = Convert.ToInt32(productKey);
                _logger.LogDebug("  ✓ Producto: {Id}", productoId);
            }
        }
    }

    
    private async Task CargarDimClientAsync(
        SqlConnection conexion,
        SqlTransaction transaccion,
        IEnumerable<Comentario> comentarios)
    {
        var clientesUnicos = comentarios
            .Where(c => !string.IsNullOrEmpty(c.ClienteId))
            .Select(c => c.ClienteId!)
            .Distinct()
            .ToList();

        foreach (var clienteId in clientesUnicos)
        {
            if (_clientKeyCache.ContainsKey(clienteId))
                continue;

            var query = @"
                IF NOT EXISTS (SELECT 1 FROM dim_client WHERE client_id = @client_id)
                BEGIN
                    INSERT INTO dim_client (
                        client_id, client_name, country, age, gender, client_type,
                        registration_date, client_segment, created_date, is_active
                    )
                    VALUES (
                        @client_id, @client_name, @country, NULL, 'Unknown', 'Regular',
                        GETUTCDATE(), 'General', GETUTCDATE(), 1
                    )
                END
                
                SELECT client_key FROM dim_client WHERE client_id = @client_id";

            using var cmd = new SqlCommand(query, conexion, transaccion);
            cmd.Parameters.AddWithValue("@client_id", clienteId);
            cmd.Parameters.AddWithValue("@client_name", $"Cliente {clienteId}");
            cmd.Parameters.AddWithValue("@country", "Unknown");

            var clientKey = await cmd.ExecuteScalarAsync();
            if (clientKey != null)
            {
                _clientKeyCache[clienteId] = Convert.ToInt32(clientKey);
                _logger.LogDebug("  ✓ Cliente: {Id}", clienteId);
            }
        }
    }

    
    private async Task CargarDimTimeAsync(
        SqlConnection conexion,
        SqlTransaction transaccion,
        IEnumerable<Comentario> comentarios)
    {
        var fechasUnicas = comentarios
            .Select(c => c.FechaCreacion.Date)
            .Distinct()
            .ToList();

        foreach (var fecha in fechasUnicas)
        {
            var timeKey = CalcularTimeKey(fecha);

            if (_timeKeyCache.ContainsKey(timeKey))
                continue;

            var query = @"
                IF NOT EXISTS (SELECT 1 FROM dim_time WHERE time_key = @time_key)
                BEGIN
                    INSERT INTO dim_time (
                        time_key, full_date, [day], [week], [month], [quarter], [year],
                        day_name, month_name, quarter_name, is_weekend, is_holiday,
                        holiday_name, is_business_day, created_date
                    )
                    VALUES (
                        @time_key, @full_date, @day, @week, @month, @quarter, @year,
                        @day_name, @month_name, @quarter_name, @is_weekend, 0,
                        NULL, @is_business_day, GETUTCDATE()
                    )
                END
                
                SELECT @time_key";

            using var cmd = new SqlCommand(query, conexion, transaccion);
            cmd.Parameters.AddWithValue("@time_key", timeKey);
            cmd.Parameters.AddWithValue("@full_date", fecha);
            cmd.Parameters.AddWithValue("@day", fecha.Day);
            cmd.Parameters.AddWithValue("@week", ObtenerSemanaDelAnio(fecha));
            cmd.Parameters.AddWithValue("@month", fecha.Month);
            cmd.Parameters.AddWithValue("@quarter", (fecha.Month - 1) / 3 + 1);
            cmd.Parameters.AddWithValue("@year", fecha.Year);
            cmd.Parameters.AddWithValue("@day_name", ObtenerNombreDia(fecha.DayOfWeek));
            cmd.Parameters.AddWithValue("@month_name", ObtenerNombreMes(fecha.Month));
            cmd.Parameters.AddWithValue("@quarter_name", $"Q{(fecha.Month - 1) / 3 + 1}");
            cmd.Parameters.AddWithValue("@is_weekend", EsFinDeSemana(fecha));
            cmd.Parameters.AddWithValue("@is_business_day", !EsFinDeSemana(fecha));

            await cmd.ExecuteScalarAsync();
            _timeKeyCache[timeKey] = timeKey;
            _logger.LogDebug("  ✓ Fecha: {Fecha}", fecha.ToString("yyyy-MM-dd"));
        }
    }

   
    private async Task<int> CargarFactOpinionsAsync(
        SqlConnection conexion,
        SqlTransaction transaccion,
        IEnumerable<Comentario> comentarios)
    {
        var dataTable = ConvertirADataTable(comentarios);

        using var bulkCopy = new SqlBulkCopy(conexion, SqlBulkCopyOptions.Default, transaccion)
        {
            DestinationTableName = "fact_opinions",
            BatchSize = 1000,
            BulkCopyTimeout = 60
        };

        // Mapear columnas
        bulkCopy.ColumnMappings.Add("client_key", "client_key");
        bulkCopy.ColumnMappings.Add("product_key", "product_key");
        bulkCopy.ColumnMappings.Add("time_key", "time_key");
        bulkCopy.ColumnMappings.Add("source_key", "source_key");
        bulkCopy.ColumnMappings.Add("sentiment_key", "sentiment_key");
        bulkCopy.ColumnMappings.Add("rating", "rating");
        bulkCopy.ColumnMappings.Add("sentiment_score", "sentiment_score");
        bulkCopy.ColumnMappings.Add("comment_text", "comment_text");
        bulkCopy.ColumnMappings.Add("comment_length", "comment_length");
        bulkCopy.ColumnMappings.Add("word_count", "word_count");
        bulkCopy.ColumnMappings.Add("contains_keywords", "contains_keywords");
        bulkCopy.ColumnMappings.Add("comment_type", "comment_type");
        bulkCopy.ColumnMappings.Add("channel_code", "channel_code");
        bulkCopy.ColumnMappings.Add("created_date", "created_date");

        await bulkCopy.WriteToServerAsync(dataTable);

        return dataTable.Rows.Count;
    }

    private DataTable ConvertirADataTable(IEnumerable<Comentario> comentarios)
    {
        var table = new DataTable();

        // Definir columnas
        table.Columns.Add("client_key", typeof(int));
        table.Columns.Add("product_key", typeof(int));
        table.Columns.Add("time_key", typeof(int));
        table.Columns.Add("source_key", typeof(int));
        table.Columns.Add("sentiment_key", typeof(int));
        table.Columns.Add("rating", typeof(decimal));
        table.Columns.Add("sentiment_score", typeof(decimal));
        table.Columns.Add("comment_text", typeof(string));
        table.Columns.Add("comment_length", typeof(int));
        table.Columns.Add("word_count", typeof(int));
        table.Columns.Add("contains_keywords", typeof(bool));
        table.Columns.Add("comment_type", typeof(string));
        table.Columns.Add("channel_code", typeof(string));
        table.Columns.Add("created_date", typeof(DateTime));

        foreach (var comentario in comentarios)
        {
            var row = table.NewRow();

            // Obtener llaves de dimensiones
            row["client_key"] = string.IsNullOrEmpty(comentario.ClienteId)
                ? 1
                : _clientKeyCache.GetValueOrDefault(comentario.ClienteId, 1);
            row["product_key"] = _productKeyCache.GetValueOrDefault(comentario.ProductoId, 1);
            row["time_key"] = CalcularTimeKey(comentario.FechaCreacion);
            row["source_key"] = _sourceKeyCache.GetValueOrDefault(comentario.Fuente, 1);
            row["sentiment_key"] = _sentimentKeyCache.GetValueOrDefault((int)comentario.Sentimiento, 3);

            // Métricas
            row["rating"] = comentario.Calificacion ?? (object)DBNull.Value;
            row["sentiment_score"] = CalcularSentimentScore(comentario.Sentimiento);
            row["comment_text"] = comentario.Texto;
            row["comment_length"] = comentario.Texto.Length;
            row["word_count"] = comentario.Texto.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;
            row["contains_keywords"] = false;
            row["comment_type"] = "Review";
            row["channel_code"] = comentario.TipoFuente.ToString();
            row["created_date"] = DateTime.UtcNow;

            table.Rows.Add(row);
        }

        return table;
    }

    

    private int CalcularTimeKey(DateTime fecha)
    {
        return fecha.Year * 10000 + fecha.Month * 100 + fecha.Day;
    }

    private decimal CalcularSentimentScore(Sentimiento sentimiento)
    {
        return sentimiento switch
        {
            Sentimiento.MuyNegativo => -0.8m,
            Sentimiento.Negativo => -0.4m,
            Sentimiento.Neutral => 0.0m,
            Sentimiento.Positivo => 0.4m,
            Sentimiento.MuyPositivo => 0.8m,
            _ => 0.0m
        };
    }

    private string ObtenerNombreSentimiento(Sentimiento sentimiento)
    {
        return sentimiento switch
        {
            Sentimiento.MuyNegativo => "Muy Negativo",
            Sentimiento.Negativo => "Negativo",
            Sentimiento.Neutral => "Neutral",
            Sentimiento.Positivo => "Positivo",
            Sentimiento.MuyPositivo => "Muy Positivo",
            _ => "Desconocido"
        };
    }

    private (decimal min, decimal max) ObtenerRangoSentimiento(Sentimiento sentimiento)
    {
        return sentimiento switch
        {
            Sentimiento.MuyNegativo => (-1.0m, -0.6m),
            Sentimiento.Negativo => (-0.6m, -0.2m),
            Sentimiento.Neutral => (-0.2m, 0.2m),
            Sentimiento.Positivo => (0.2m, 0.6m),
            Sentimiento.MuyPositivo => (0.6m, 1.0m),
            _ => (0.0m, 0.0m)
        };
    }

    private string ObtenerColorSentimiento(Sentimiento sentimiento)
    {
        return sentimiento switch
        {
            Sentimiento.MuyNegativo => "#DC3545",
            Sentimiento.Negativo => "#FD7E14",
            Sentimiento.Neutral => "#FFC107",
            Sentimiento.Positivo => "#28A745",
            Sentimiento.MuyPositivo => "#198754",
            _ => "#6C757D"
        };
    }

    private string ObtenerCategoriaPorTipo(TipoFuente tipo)
    {
        return tipo switch
        {
            TipoFuente.CSV => "Encuestas Internas",
            TipoFuente.BaseDeDatos => "Reseñas Web",
            TipoFuente.API => "Redes Sociales",
            _ => "Desconocido"
        };
    }

    private string ObtenerNombreDia(DayOfWeek dia)
    {
        return dia switch
        {
            DayOfWeek.Monday => "Lunes",
            DayOfWeek.Tuesday => "Martes",
            DayOfWeek.Wednesday => "Miércoles",
            DayOfWeek.Thursday => "Jueves",
            DayOfWeek.Friday => "Viernes",
            DayOfWeek.Saturday => "Sábado",
            DayOfWeek.Sunday => "Domingo",
            _ => "Desconocido"
        };
    }

    private string ObtenerNombreMes(int mes)
    {
        return mes switch
        {
            1 => "Enero",
            2 => "Febrero",
            3 => "Marzo",
            4 => "Abril",
            5 => "Mayo",
            6 => "Junio",
            7 => "Julio",
            8 => "Agosto",
            9 => "Septiembre",
            10 => "Octubre",
            11 => "Noviembre",
            12 => "Diciembre",
            _ => "Desconocido"
        };
    }

    private int ObtenerSemanaDelAnio(DateTime fecha)
    {
        return System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
            fecha,
            System.Globalization.CalendarWeekRule.FirstDay,
            DayOfWeek.Monday);
    }

    private bool EsFinDeSemana(DateTime fecha)
    {
        return fecha.DayOfWeek == DayOfWeek.Saturday || fecha.DayOfWeek == DayOfWeek.Sunday;
    }
}