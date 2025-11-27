using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities.Auditing;

namespace ContableWeb.Entities.TiposDocumentos;

[Audited]
public class TipoDocumento : AuditedEntity<int>
{
    public int CodigoAfip { get; set; } // Id del DocTipo en AFIP
    public required string Descripcion { get; set; } // Desc del DocTipo
    public DateOnly? FechaDesde { get; set; } // FchDesde del DocTipo
    public DateOnly? FechaHasta { get; set; } // FchHasta del DocTipo
    public bool Enabled { get; set; } = true;
}

