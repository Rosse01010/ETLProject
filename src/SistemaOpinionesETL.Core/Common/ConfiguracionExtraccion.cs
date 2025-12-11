using System.Collections.Generic;

namespace SistemaOpinionesETL.Core.Common
{
    public class ConfiguracionExtraccion
    {
        public CSVConfig CSV { get; set; } = new CSVConfig();
        public ConfiguracionBaseDatos BaseDatos { get; set; } = new ConfiguracionBaseDatos();
        public ConfiguracionAPI API { get; set; } = new ConfiguracionAPI();
        public ConfiguracionAlmacenamiento Almacenamiento { get; set; } = new ConfiguracionAlmacenamiento();
    }

    public class CSVConfig
    {
        // Propiedad existente para compatibilidad
        public string RutaArchivo { get; set; } = "";

        // Nueva propiedad para múltiples archivos
        public List<string> RutasArchivos { get; set; } = new List<string>();

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
}
