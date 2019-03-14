using AutoMapper;
using BlackSlope.Hosts.Api.Operations.Movies.ViewModels;
using BlackSlope.Services.MovieService.DomainModels;

namespace BlackSlope.Hosts.Api.Operations.Movies.MapperProfiles
{
    public class CreateMovieRequestProfile : Profile
    {
        public CreateMovieRequestProfile()
        {
            CreateMap<CreateMovieRequestViewModel, MovieDomainModel>().ReverseMap();
        }
    }
}
