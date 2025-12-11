
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using SistemaOpinionesETL.Core.Entities;

namespace SistemaOpinionesETL.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ComentariosController : ControllerBase
{
    private readonly ILogger<ComentariosController> _logger;
    private readonly IWebHostEnvironment _environment;

    public ComentariosController(
        ILogger<ComentariosController> logger,
        IWebHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Obtiene comentarios de redes sociales (simulado desde CSV)
    /// </summary>
    [HttpGet("redes-sociales")]
    public async Task<ActionResult<IEnumerable<Comentario>>> ObtenerComentariosRedesSociales()
    {
        try
        {
            var rutaArchivo = Path.Combine(_environment.ContentRootPath, "Data", "ComentariosRedesSociales.csv");

            if (!System.IO.File.Exists(rutaArchivo))
            {
                _logger.LogWarning("Archivo de redes sociales no encontrado: {Ruta}", rutaArchivo);
                return Ok(GenerarComentariosDePrueba("RedesSociales"));
            }

            var comentarios = await LeerComentariosDesdeCSV(rutaArchivo, "RedesSociales");
            return Ok(comentarios);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo comentarios de redes sociales");
            return StatusCode(500, new { Error = "Error al obtener comentarios" });
        }
    }

    /// <summary>
    /// Obtiene todos los comentarios del staging
    /// </summary>
    [HttpGet("staging")]
    public async Task<ActionResult<IEnumerable<Comentario>>> ObtenerComentariosStaging()
    {
        try
        {
            var directorioStaging = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "staging");

            if (!Directory.Exists(directorioStaging))
            {
                return Ok(new List<Comentario>());
            }

            var archivos = Directory.GetFiles(directorioStaging, "staging_*.json");
            var todosComentarios = new List<Comentario>();

            foreach (var archivo in archivos.Take(10)) // Limitar a 10 archivos
            {
                var json = await System.IO.File.ReadAllTextAsync(archivo);
                var comentarios = JsonSerializer.Deserialize<IEnumerable<Comentario>>(json);
                if (comentarios != null)
                {
                    todosComentarios.AddRange(comentarios);
                }
            }

            return Ok(todosComentarios);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo comentarios de staging");
            return StatusCode(500, new { Error = "Error al obtener comentarios" });
        }
    }

    /// <summary>
    /// Obtiene estadísticas de los comentarios
    /// </summary>
    [HttpGet("estadisticas")]
    public ActionResult<object> ObtenerEstadisticas()
    {
        try
        {
            var directorioStaging = Path.Combine(Directory.GetCurrentDirectory(), "..", "..", "staging");
            var archivosStaging = Directory.Exists(directorioStaging)
                ? Directory.GetFiles(directorioStaging, "staging_*.json").Length
                : 0;

            var estadisticas = new
            {
                ArchivosStaging = archivosStaging,
                Timestamp = DateTime.UtcNow,
                Estado = "Operacional"
            };

            return Ok(estadisticas);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error obteniendo estadísticas");
            return StatusCode(500, new { Error = "Error al obtener estadísticas" });
        }
    }

    private async Task<IEnumerable<Comentario>> LeerComentariosDesdeCSV(string rutaArchivo, string fuente)
    {
        var comentarios = new List<Comentario>();
        var lineas = await System.IO.File.ReadAllLinesAsync(rutaArchivo);

        // Saltar encabezado
        foreach (var linea in lineas.Skip(1))
        {
            var campos = linea.Split(',');
            if (campos.Length >= 5)
            {
                comentarios.Add(new Comentario
                {
                    ProductoId = campos[0],
                    ClienteId = campos[1],
                    FechaCreacion = DateTime.TryParse(campos[2], out var fecha) ? fecha : DateTime.Now,
                    Texto = campos[3],
                    Calificacion = decimal.TryParse(campos[4], out var rating) ? rating : null,
                    Fuente = fuente,
                    TipoFuente = Core.Common.TipoFuente.API
                });
            }
        }

        return comentarios;
    }

    private IEnumerable<Comentario> GenerarComentariosDePrueba(string fuente)
    {
        return new List<Comentario>
        {
            new Comentario
            {
                ProductoId = "PROD001",
                ClienteId = "CUST001",
                FechaCreacion = DateTime.Now.AddDays(-1),
                Texto = "Excelente producto, muy recomendado!",
                Calificacion = 5,
                Fuente = fuente,
                TipoFuente = Core.Common.TipoFuente.API
            },
            new Comentario
            {
                ProductoId = "PROD002",
                ClienteId = "CUST002",
                FechaCreacion = DateTime.Now.AddHours(-12),
                Texto = "Buena calidad pero un poco caro",
                Calificacion = 4,
                Fuente = fuente,
                TipoFuente = Core.Common.TipoFuente.API
            }
        };
    }
}