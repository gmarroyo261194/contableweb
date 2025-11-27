using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities.Auditing;

namespace ContableWeb.Entities.TiposCondicionesIva;

[Audited]
public class TipoCondicionIva : AuditedEntity<int>
{
    public int CodigoAfip { get; set; } // Id del CondicionIva en AFIP
    public required string Descripcion { get; set; } // Desc del CondicionIva
    public bool Enabled { get; set; } = true;
}

