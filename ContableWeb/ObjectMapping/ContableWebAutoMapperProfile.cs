using AutoMapper;
using ContableWeb.Entities.Books;
using ContableWeb.Entities.Rubros;
using ContableWeb.Services.Dtos.Books;
using ContableWeb.Services.Dtos.Rubros;

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

        /* Create your AutoMapper object mappings here */
    }
}
