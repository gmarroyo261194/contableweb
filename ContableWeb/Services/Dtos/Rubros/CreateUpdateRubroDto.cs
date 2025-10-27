using System.ComponentModel.DataAnnotations;

namespace ContableWeb.Services.Dtos.Rubros;

public class CreateUpdateRubroDto
{
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = string.Empty;
    
    public bool Enabled { get; set; } = true;
}