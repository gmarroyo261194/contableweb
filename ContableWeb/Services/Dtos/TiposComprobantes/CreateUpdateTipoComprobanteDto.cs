using System.ComponentModel.DataAnnotations;

namespace ContableWeb.Services.Dtos.TiposComprobantes;

public class CreateUpdateTipoComprobanteDto
{
    public int CodigoAfip { get; set; }
    [Required] [StringLength(200)] public string Nombre { get; set; } = null!;
    [MaxLength(5)] public string? Abreviatura { get; set; }
    public DateOnly? FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public bool EsFiscal { get; set; }
    public bool Enabled { get; set; }
}