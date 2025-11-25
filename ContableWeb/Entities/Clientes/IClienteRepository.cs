using ContableWeb.Services.Clientes;
using Volo.Abp.Domain.Repositories;

namespace ContableWeb.Entities.Clientes;

public interface IClienteRepository: IRepository<Cliente, int>
{
    Task<List<Cliente>> GetListAsync(int skipCount, int maxResultCount, string sorting= "Nombre", ClientesFilter? filter = null);
    Task<int> GetTotalCountAsync(ClientesFilter filter);
}