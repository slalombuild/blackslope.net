using AutoMapper;
using BlackSlope.Repositories.Movies.DtoModels;
using BlackSlope.Services.Movies.DomainModels;

namespace BlackSlope.Services.Movies.MapperProfiles
{
    public class MovieProfile : Profile
    {
        public MovieProfile()
        {
            CreateMap<MovieDomainModel, MovieDtoModel>().ReverseMap();
        }
    }
}
