
using SistemaOpinionesETL.Core.Common;
namespace SistemaOpinionesETL.Core.Interfaces;

/// <summary>
/// Interfaz base para todos los extractores de datos.
/// Patrón Strategy para fácil escalabilidad.
/// </summary>
public interface IExtractor
{
    TipoFuente TipoFuente { get; }
    string Nombre { get; }
    Task<ResultadoExtraccion> ExtraerAsync(CancellationToken cancellationToken = default);
    bool ValidarConfiguracion();
}