using System.ComponentModel.DataAnnotations;

namespace ContableWeb.Services.Dtos.TiposDocumentos;

public class CreateUpdateTipoDocumentoDto
{
    public int CodigoAfip { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Descripcion { get; set; } = null!;
    
    public DateOnly? FechaDesde { get; set; }
    public DateOnly? FechaHasta { get; set; }
    public bool Enabled { get; set; } = true;
}

