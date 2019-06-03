using AutoMapper;
using BlackSlope.Api.Operations.Movies.ViewModels;
using BlackSlope.Services.Movies.DomainModels;

namespace BlackSlope.Api.Operations.Movies.MapperProfiles
{
    public class UpdateMovieRequestProfile : Profile
    {
        public UpdateMovieRequestProfile()
        {
            CreateMap<UpdateMovieViewModel, MovieDomainModel>().ReverseMap();
        }
    }
}
