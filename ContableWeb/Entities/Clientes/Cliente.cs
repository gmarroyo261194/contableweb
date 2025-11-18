using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities.Auditing;

namespace ContableWeb.Entities.Clientes;

[Audited]
public class Cliente: FullAuditedEntity<int>
{
    public required string Nombre { get; set; }
    public TipoDoc TipoDocumento { get; set; }
    public required string NumeroDocumento { get; set; }
    public string? Domicilio { get; set; }
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public string? Observaciones { get; set; }
    public TipoCondIva CondicionIva { get; set; }
    public bool Enabled { get; set; } = true;
}