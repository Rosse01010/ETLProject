using CsvHelper.Configuration;
using SistemaOpinionesETL.Core.Entities;
using System.Globalization;

namespace SistemaOpinionesETL.Infrastructure.Extractors;

// Mapeo para surveys_part1.csv
public class SurveysCsvMap : ClassMap<Comentario>
{
    public SurveysCsvMap()
    {
        Map(m => m.Id).Ignore();
        Map(m => m.ProductoId).Name("IdProducto");
        Map(m => m.ClienteId).Name("IdCliente");
        Map(m => m.FechaCreacion).Name("Fecha")
            .TypeConverterOption.Format("yyyy-MM-dd");
        Map(m => m.Texto).Name("Comentario");
        Map(m => m.Calificacion).Name("PuntajeSatisfacción", "Clasificación")
            .TypeConverterOption.CultureInfo(CultureInfo.InvariantCulture);
        Map(m => m.Fuente).Name("Fuente");
        Map(m => m.TipoFuente).Convert(_ => Core.Common.TipoFuente.CSV);
        Map(m => m.FechaImportacion).Convert(_ => DateTime.UtcNow);
    }
}

// Mapeo para clients.csv
public class ClientsCsvMap : ClassMap<Comentario>
{
    public ClientsCsvMap()
    {
        Map(m => m.Id).Ignore();
        Map(m => m.ClienteId).Name("IdCliente");
        Map(m => m.Texto).Constant("Cliente");
        Map(m => m.Fuente).Constant("Clientes CSV");
        Map(m => m.TipoFuente).Convert(_ => Core.Common.TipoFuente.CSV);
        Map(m => m.FechaCreacion).Convert(_ => DateTime.UtcNow);
        Map(m => m.FechaImportacion).Convert(_ => DateTime.UtcNow);
    }
}

// Mapeo para products.csv
public class ProductsCsvMap : ClassMap<Comentario>
{
    public ProductsCsvMap()
    {
        Map(m => m.Id).Ignore();
        Map(m => m.ProductoId).Name("IdProducto");
        Map(m => m.Texto).Name("Nombre");
        Map(m => m.Fuente).Constant("Productos CSV");
        Map(m => m.TipoFuente).Convert(_ => Core.Common.TipoFuente.CSV);
        Map(m => m.FechaCreacion).Convert(_ => DateTime.UtcNow);
        Map(m => m.FechaImportacion).Convert(_ => DateTime.UtcNow);
    }
}

// Mapeo para fuente_datos.csv
public class FuenteDatosCsvMap : ClassMap<Comentario>
{
    public FuenteDatosCsvMap()
    {
        Map(m => m.Id).Ignore();
        Map(m => m.Fuente).Name("TipoFuente");
        Map(m => m.TipoFuente).Convert(_ => Core.Common.TipoFuente.CSV);
        Map(m => m.FechaCreacion).Name("FechaCarga")
            .TypeConverterOption.Format("yyyy-MM-dd");
        Map(m => m.FechaImportacion).Convert(_ => DateTime.UtcNow);
    }
}
