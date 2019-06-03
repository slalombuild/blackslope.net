using AutoMapper;
using BlackSlope.Api.Operations.Movies.ViewModels;
using BlackSlope.Services.Movies.DomainModels;

namespace BlackSlope.Api.Operations.Movies.MapperProfiles
{
    public class MovieResponseProfile : Profile
    {
        public MovieResponseProfile()
        {
            CreateMap<MovieDomainModel, MovieViewModel>().ReverseMap();
        }
    }
}
