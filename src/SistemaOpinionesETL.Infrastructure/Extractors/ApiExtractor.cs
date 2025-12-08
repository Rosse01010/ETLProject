
// ========================================
// ARCHIVO: src/SistemaOpinionesETL.Infrastructure/Extractors/ApiExtractor.cs
// ========================================
using System.Diagnostics;
using System.Text.Json;
using SistemaOpinionesETL.Core.Common;
using SistemaOpinionesETL.Core.Entities;
using SistemaOpinionesETL.Core.Interfaces;
using Microsoft.Extensions.Logging;

namespace SistemaOpinionesETL.Infrastructure.Extractors;

public class ApiExtractor : IExtractor
{
    private readonly string _urlApi;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiExtractor> _logger;

    public TipoFuente TipoFuente => TipoFuente.API;
    public string Nombre => "API Extractor";

    public ApiExtractor(string urlApi, HttpClient httpClient, ILogger<ApiExtractor> logger)
    {
        _urlApi = urlApi ?? throw new ArgumentNullException(nameof(urlApi));
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public bool ValidarConfiguracion()
    {
        if (string.IsNullOrWhiteSpace(_urlApi))
        {
            _logger.LogError("URL de API no configurada");
            return false;
        }

        if (!Uri.TryCreate(_urlApi, UriKind.Absolute, out _))
        {
            _logger.LogError("URL de API inválida: {Url}", _urlApi);
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
            _logger.LogInformation("Iniciando extracción desde API: {Url}", _urlApi);

            if (!ValidarConfiguracion())
            {
                resultado.Exitoso = false;
                resultado.MensajeError = "Configuración inválida";
                return resultado;
            }

            var respuesta = await _httpClient.GetAsync(_urlApi, cancellationToken);
            respuesta.EnsureSuccessStatusCode();

            var json = await respuesta.Content.ReadAsStringAsync(cancellationToken);
            var opciones = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var comentarios = JsonSerializer.Deserialize<IEnumerable<Comentario>>(json, opciones);

            if (comentarios != null)
            {
                foreach (var comentario in comentarios)
                {
                    comentario.TipoFuente = TipoFuente.API;
                    comentario.FechaImportacion = DateTime.UtcNow;
                }
            }

            resultado.Comentarios = comentarios ?? Enumerable.Empty<Comentario>();
            resultado.Exitoso = true;

            _logger.LogInformation("✅ API: {Registros} registros extraídos en {Tiempo}ms",
                resultado.RegistrosExtraidos, stopwatch.ElapsedMilliseconds);
        }
        catch (HttpRequestException ex)
        {
            resultado.Exitoso = false;
            resultado.MensajeError = $"Error HTTP: {ex.Message}";
            _logger.LogError(ex, "❌ Error HTTP en extracción de API");
        }
        catch (Exception ex)
        {
            resultado.Exitoso = false;
            resultado.MensajeError = ex.Message;
            _logger.LogError(ex, "❌ Error extrayendo datos de API");
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