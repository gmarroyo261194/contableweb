using ContableWeb.Entities.Clientes;
using ContableWeb.Services.Dtos.Clientes;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace ContableWeb.Services.Clientes;

public interface IClienteAppService: 
    ICrudAppService< //Defines CRUD methods
        ClienteDto, //Used to show clientes
        int, //Primary key of the cliente entity
        PagedAndSortedResultRequestDto, //Used for paging/sorting
        CreateUpdateClienteDto> //Used to create/update a cliente
{
    Task<bool> GetDocumentoDuplicadoAsync(TipoDoc tipoDoc, string numeroDoc,int id = 0);
}