
namespace SistemaOpinionesETL.Core.Common;

public enum TipoFuente
{
    CSV = 1,
    BaseDeDatos = 2,
    API = 3
}

public enum Sentimiento
{
    MuyNegativo = 1,
    Negativo = 2,
    Neutral = 3,
    Positivo = 4,
    MuyPositivo = 5
}