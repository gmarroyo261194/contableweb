using AutoMapper;
using ContableWeb.Entities.Books;
using ContableWeb.Entities.Clientes;
using ContableWeb.Entities.Rubros;
using ContableWeb.Entities.Servicios;
using ContableWeb.Entities.TiposComprobantes;
using ContableWeb.Entities.TiposDocumentos;
using ContableWeb.Entities.TiposCondicionesIva;
using ContableWeb.Services.Clientes;
using ContableWeb.Services.Dtos.Books;
using ContableWeb.Services.Dtos.Clientes;
using ContableWeb.Services.Dtos.Rubros;
using ContableWeb.Services.Dtos.Servicios;
using ContableWeb.Services.Dtos.TiposComprobantes;
using ContableWeb.Services.Dtos.TiposDocumentos;
using ContableWeb.Services.Dtos.TiposCondicionesIva;

namespace ContableWeb.ObjectMapping;

public class ContableWebAutoMapperProfile : Profile
{
    public ContableWebAutoMapperProfile()
    {
        CreateMap<Book, BookDto>();
        CreateMap<CreateUpdateBookDto, Book>();
        CreateMap<BookDto, CreateUpdateBookDto>();

        CreateMap<Rubro, RubroDto>();
        CreateMap<CreateUpdateRubroDto, Rubro>();
        CreateMap<RubroDto, CreateUpdateRubroDto>();
        CreateMap<Rubro, RubroLookupDto>();
        
        CreateMap<Servicio, ServicioDto>();
        CreateMap<CreateUpdateServicioDto, Servicio>();
        CreateMap<ServicioDto, CreateUpdateServicioDto>();
        
        CreateMap<TipoComprobante, TipoComprobanteDto>();
        CreateMap<CreateUpdateTipoComprobanteDto, TipoComprobante>();
        CreateMap<TipoComprobanteDto, CreateUpdateTipoComprobanteDto>();
        
        CreateMap<TipoDocumento, TipoDocumentoDto>();
        CreateMap<CreateUpdateTipoDocumentoDto, TipoDocumento>();
        CreateMap<TipoDocumentoDto, CreateUpdateTipoDocumentoDto>();
        
        CreateMap<TipoCondicionIva, TipoCondicionIvaDto>();
        CreateMap<CreateUpdateTipoCondicionIvaDto, TipoCondicionIva>();
        CreateMap<TipoCondicionIvaDto, CreateUpdateTipoCondicionIvaDto>();
        
        CreateMap<Cliente, ClienteDto>();
        CreateMap<CreateUpdateClienteDto, Cliente>();
        CreateMap<ClienteDto, CreateUpdateClienteDto>();
        CreateMap<ClientePagedAndSortedResultRequestDto, ClientesFilter>();
    }
}
