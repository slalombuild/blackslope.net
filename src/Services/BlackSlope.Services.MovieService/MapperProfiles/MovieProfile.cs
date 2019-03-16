using AutoMapper;
using BlackSlope.Repositories.MovieRepository.DtoModels;
using BlackSlope.Services.MovieService.DomainModels;

namespace BlackSlope.Services.MovieService.MapperProfiles
{
    public class MovieProfile : Profile
    {
        public MovieProfile()
        {
            CreateMap<MovieDomainModel, MovieDtoModel>().ReverseMap();
        }
    }
}
