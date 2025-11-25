using System.ComponentModel.DataAnnotations;
using ContableWeb.Entities.Clientes;
using Volo.Abp.Application.Dtos;

namespace ContableWeb.Services.Dtos.Clientes;

public class ClientePagedAndSortedResultRequestDto: PagedAndSortedResultRequestDto
{
    public string? Nombre { get; set; } = null!;

    public TipoDoc TipoDocumento { get; set; }

    public string? NumeroDocumento { get; set; } = null!;

    public string? Domicilio { get; set; }

    public string? Telefono { get; set; }
    
    public string? Email { get; set; }

    public string? Observaciones { get; set; }

    public TipoCondIva CondicionIva { get; set; }

    public bool Enabled { get; set; }
}