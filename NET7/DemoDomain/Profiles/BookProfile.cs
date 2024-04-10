using AutoMapper;
using DemoDomain.Dtos;
using DemoDomain.Entities;

namespace DemoDomain.Profiles;

public class BookProfile : Profile
{
    public BookProfile()
    {
        CreateMap<BookDto, Book>();
    }
}