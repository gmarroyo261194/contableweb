using Volo.Abp.Domain.Entities.Auditing;
using ContableWeb.Entities.Rubros;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Entities;
using Volo.Abp.Threading;

namespace ContableWeb.Entities.Servicios;

[Audited]
public class Servicio: AuditedEntity<int>, IEntity<int>
{
    public int RubroId { get; set; }
    public required string Nombre { get; set; }
    public bool Enabled { get; set; } = true;
    
    // Referencia al Rubro padre (N Servicios -> 1 Rubro)
    public Rubro? Rubro { get; set; }
}