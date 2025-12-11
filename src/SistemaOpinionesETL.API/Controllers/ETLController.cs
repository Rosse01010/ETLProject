
using Microsoft.AspNetCore.Mvc;
using SistemaOpinionesETL.Core.Interfaces;

namespace SistemaOpinionesETL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ETLController : ControllerBase
{
    private readonly IOrquestadorExtraccion _orquestador;
    private readonly IDataStagingService _stagingService;
    private readonly ILogger<ETLController> _logger;

    public ETLController(
        IOrquestadorExtraccion orquestador,
        IDataStagingService stagingService,
        ILogger<ETLController> logger)
    {
        _orquestador = orquestador;
        _stagingService = stagingService;
        _logger = logger;
    }

    /// <summary>
    /// Ejecuta manualmente el proceso de extracción
    /// </summary>
    [HttpPost("ejecutar")]
    public async Task<IActionResult> EjecutarExtraccion()
    {
        try
        {
            _logger.LogInformation("Ejecución manual de ETL iniciada");

            var resumen = await _orquestador.EjecutarExtraccionAsync();

            return Ok(new
            {
                Exitoso = true,
                Mensaje = "Extracción completada exitosamente",
                Resumen = resumen,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error en ejecución manual de ETL");
            return StatusCode(500, new
            {
                Exitoso = false,
                Mensaje = "Error en la extracción",
                Error = ex.Message,
                Timestamp = DateTime.UtcNow
            });
        }
    }

    /// <summary>
    /// Obtiene el estado actual del proceso ETL
    /// </summary>
    [HttpGet("estado")]
    public async Task<IActionResult> ObtenerEstado()
    {
        try
        {
            var archivosStaging = _stagingService.ObtenerArchivosStaging();
            var cantidadStaging = await _stagingService.ObtenerCantidadAlmacenadaAsync();

            return Ok(new
            {
                Estado = "Operacional",
                ArchivosEnStaging = cantidadStaging,
                UltimosArchivos = archivosStaging.Take(5).Select(Path.GetFileName),
                Timestamp = DateTime.UtcNow,
                Metricas = new
                {
                    Memoria = $"{GC.GetTotalMemory(false) / 1024 / 1024} MB",
                    Procesadores = Environment.ProcessorCount
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo estado del ETL");
            return StatusCode(500, new { Error = ex.Message });
        }
    }

    /// <summary>
    /// Health check del servicio ETL
    /// </summary>
    [HttpGet("health")]
    public IActionResult HealthCheck()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "Sistema Opiniones ETL",
            Version = "1.0.0",
            Timestamp = DateTime.UtcNow
        });
    }
}