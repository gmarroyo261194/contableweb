using System;
using Volo.Abp.Application.Dtos;
using ContableWeb.Entities.Books;

namespace ContableWeb.Services.Dtos.Books;

public class BookDto : AuditedEntityDto<Guid>
{
    public string Name { get; set; }

    public BookType Type { get; set; }

    public DateTime PublishDate { get; set; }

    public float Price { get; set; }
}