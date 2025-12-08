
using SistemaOpinionesETL.Core.Entities;
namespace SistemaOpinionesETL.Core.Interfaces;

public interface IDataLoader
{
    Task<int> CargarABaseAnaliticaAsync(IEnumerable<Comentario> comentarios);
    Task<int> CargarDesdeStagingAsync(string rutaArchivo);
    Task<bool> ValidarConexionAsync();
}