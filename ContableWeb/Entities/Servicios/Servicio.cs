using Volo.Abp.Domain.Entities.Auditing;
using ContableWeb.Entities.Rubros;

namespace ContableWeb.Entities.Servicios;

public class Servicio: AuditedEntity<int>
{
    public int RubroId { get; set; }
    public required string Nombre { get; set; }
    public bool Enabled { get; set; } = true;
    
    // Referencia al Rubro padre (N Servicios -> 1 Rubro)
    public Rubro? Rubro { get; set; }
}