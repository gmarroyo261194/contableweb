using System.ComponentModel.DataAnnotations;

namespace ContableWeb.Services.Dtos.Servicios;

public class CreateUpdateServicioDto
{
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;
    [Required]
    public int RubroId { get; set; }
    public bool Enabled { get; set; } = true;
}