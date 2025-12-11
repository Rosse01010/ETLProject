
using SistemaOpinionesETL.Core.Common;
namespace SistemaOpinionesETL.Core.Interfaces;

public interface IOrquestadorExtraccion
{
    Task<ResumenExtraccion> EjecutarExtraccionAsync();
}