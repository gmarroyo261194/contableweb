using Volo.Abp.Domain.Entities.Auditing;

namespace ContableWeb.Entities.Rubros;

public class Rubro: AuditedEntity<int>
{
    public required string Nombre { get; set; }
    public bool Enabled { get; set; } = true;
}