using Volo.Abp.Application.Dtos;

namespace ContableWeb.Services.Dtos.Servicios;

public class ServicioDto:  AuditedEntityDto<int>
{
    public string Nombre { get; set; } = null!;
    public int RubroId { get; set; }
    public string RubroNombre { get; set; } = null!;
    public bool Enabled { get; set; } = true;
    
}