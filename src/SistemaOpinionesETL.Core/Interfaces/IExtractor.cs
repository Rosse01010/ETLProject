
using SistemaOpinionesETL.Core.Common;
namespace SistemaOpinionesETL.Core.Interfaces;


public interface IExtractor
{
    TipoFuente TipoFuente { get; }
    string Nombre { get; }
    Task<ResultadoExtraccion> ExtraerAsync(CancellationToken cancellationToken = default);
    bool ValidarConfiguracion();
}