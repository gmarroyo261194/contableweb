using System.ComponentModel.DataAnnotations;
using ContableWeb.Entities.Clientes;

namespace ContableWeb.Services.Dtos.Clientes;

public class CreateUpdateClienteDto
{
    [Required]
    public string Nombre { get; set; } = null!;
    [Required]
    public TipoDoc TipoDocumento { get; set; }
    [Required]
    public string NumeroDocumento { get; set; } = null!;
    public string? Domicilio { get; set; }
    public string? Telefono { get; set; }
    [EmailAddress]
    public string? Email { get; set; }
    public string? Observaciones { get; set; }
    [Required]
    public TipoCondIva CondicionIva { get; set; }
    public bool Enabled { get; set; } = true;
}