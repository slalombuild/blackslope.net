using AutoMapper;
using BlackSlope.Hosts.Api.Operations.Movies.ViewModels;
using BlackSlope.Services.MovieService.DomainModels;

namespace BlackSlope.Hosts.Api.Operations.Movies.MapperProfiles
{
    public class UpdateMovieRequestProfile : Profile
    {
        public UpdateMovieRequestProfile()
        {
            CreateMap<UpdateMovieRequestViewModel, MovieDomainModel>().ReverseMap();
        }
    }
}
