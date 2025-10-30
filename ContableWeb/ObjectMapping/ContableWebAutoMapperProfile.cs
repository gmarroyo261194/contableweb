using AutoMapper;
using ContableWeb.Entities.Books;
using ContableWeb.Entities.Rubros;
using ContableWeb.Entities.Servicios;
using ContableWeb.Services.Dtos.Books;
using ContableWeb.Services.Dtos.Rubros;
using ContableWeb.Services.Dtos.Servicios;

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
    }
}
