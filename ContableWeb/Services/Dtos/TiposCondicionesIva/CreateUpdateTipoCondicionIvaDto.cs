using System.ComponentModel.DataAnnotations;

namespace ContableWeb.Services.Dtos.TiposCondicionesIva;

public class CreateUpdateTipoCondicionIvaDto
{
    public int CodigoAfip { get; set; }
    
    [Required]
    [StringLength(100)]
    public string Descripcion { get; set; } = null!;
    
    public bool Enabled { get; set; } = true;
}

