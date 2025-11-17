using ContableWeb.Entities.Clientes;
using ContableWeb.Services.Dtos.Clientes;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Domain.Repositories;

namespace ContableWeb.Services.Clientes;

public class ClienteAppService: CrudAppService<Cliente, ClienteDto, int, PagedAndSortedResultRequestDto, CreateUpdateClienteDto>,
    IClienteAppService
{
    public ClienteAppService(IRepository<Cliente, int> repository) : base(repository)
    {
    }

    public override async Task<ClienteDto> CreateAsync(CreateUpdateClienteDto input)
    {
        if(await GetDocumentoDuplicadoAsync(input.TipoDocumento, input.NumeroDocumento))
        {
            throw new UserFriendlyException("Datos Duplicados", "DocumentoDuplicado",$"Existe un cliente en con el mismo tipo {input.TipoDocumento} y número {input.NumeroDocumento}.");
        }
        return await base.CreateAsync(input);
    }

    public async Task<bool> GetDocumentoDuplicadoAsync(TipoDoc tipoDoc, string numeroDoc)
    {
        var count = await Repository.CountAsync(c => c.TipoDocumento == tipoDoc && c.NumeroDocumento == numeroDoc);
        return count > 0;
    }
}