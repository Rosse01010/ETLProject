
namespace SistemaOpinionesETL.Core.Common;

public class ConfiguracionExtraccion
{
    public ConfiguracionCSV CSV { get; set; } = new();
    public ConfiguracionBaseDatos BaseDatos { get; set; } = new();
    public ConfiguracionAPI API { get; set; } = new();
    public ConfiguracionAlmacenamiento Almacenamiento { get; set; } = new();
}

public class ConfiguracionCSV
{
    public string RutaArchivo { get; set; } = string.Empty;
    public string Separador { get; set; } = ",";
    public bool TieneEncabezado { get; set; } = true;
}

public class ConfiguracionBaseDatos
{
    public string ConnectionString { get; set; } = string.Empty;
    public string Query { get; set; } = string.Empty;
    public int TimeoutSegundos { get; set; } = 30;
}

public class ConfiguracionAPI
{
    public string UrlBase { get; set; } = string.Empty;
    public string Endpoint { get; set; } = string.Empty;
    public int TimeoutSegundos { get; set; } = 30;
}

public class ConfiguracionAlmacenamiento
{
    public string DirectorioStaging { get; set; } = "staging";
    public int DiasRetencion { get; set; } = 30;
}