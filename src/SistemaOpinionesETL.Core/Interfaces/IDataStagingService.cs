
using SistemaOpinionesETL.Core.Entities;
namespace SistemaOpinionesETL.Core.Interfaces;

public interface IDataStagingService
{
    Task<string?> GuardarEnAlmacenamientoAsync(IEnumerable<Comentario> comentarios);
    Task<int> ObtenerCantidadAlmacenadaAsync();
    IEnumerable<string> ObtenerArchivosStaging();
    Task LimpiarArchivosViejosAsync(int diasRetencion = 7);
}