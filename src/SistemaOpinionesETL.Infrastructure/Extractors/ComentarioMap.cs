using CsvHelper.Configuration;
using SistemaOpinionesETL.Core.Common;
using SistemaOpinionesETL.Core.Entities;

namespace SistemaOpinionesETL.Infrastructure.Extractors;

public class ComentarioMap : ClassMap<Comentario>
{
    public ComentarioMap()
    {
        Map(m => m.ProductoId).Name("ProductId", "IdProducto");
        Map(m => m.ClienteId).Name("CustomerId", "IdCliente").Optional();
        Map(m => m.FechaCreacion).Name("CreatedAt", "Fecha");
        Map(m => m.Texto).Name("Text", "Comentario", "CommentText");
        Map(m => m.Calificacion).Name("Rating", "Calificacion").Optional();
        Map(m => m.Fuente).Name("Source", "Fuente");
        Map(m => m.TipoFuente).Convert(_ => TipoFuente.CSV);
        Map(m => m.FechaImportacion).Convert(_ => DateTime.UtcNow);
    }
}