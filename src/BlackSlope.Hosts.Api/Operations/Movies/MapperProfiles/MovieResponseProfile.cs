using AutoMapper;
using BlackSlope.Hosts.Api.Operations.Movies.ViewModels;
using BlackSlope.Services.MovieService.DomainModels;

namespace BlackSlope.Hosts.Api.Operations.Movies.MapperProfiles
{
    public class MovieResponseProfile : Profile
    {
        public MovieResponseProfile()
        {
            CreateMap<MovieDomainModel, MovieResponseViewModel>().ReverseMap();
        }
    }
}
