using Volo.Abp.Domain.Entities.Auditing;
using ContableWeb.Entities.Servicios;
using System.Collections.Generic;

namespace ContableWeb.Entities.Rubros;

public class Rubro: AuditedEntity<int>
{
    public required string Nombre { get; set; }
    public bool Enabled { get; set; } = true;

    // Colección de servicios asociados (1 Rubro -> N Servicios)
    public ICollection<Servicio> Servicios { get; set; } = new List<Servicio>();
}