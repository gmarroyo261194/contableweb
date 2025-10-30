using System.ComponentModel.DataAnnotations;

namespace ContableWeb.Services.Dtos.TiposComprobantes;

public class CreateUpdateTipoComprobanteDto
{
    [Required]
    [StringLength(100)]
    public string Nombre { get; set; } = null!;
    public string? Descripcion { get; set; }
    [MaxLength(5)]
    public string? Abreviatura { get; set; }
    public bool EsFiscal { get; set; }
    public bool Enabled { get; set; }
}