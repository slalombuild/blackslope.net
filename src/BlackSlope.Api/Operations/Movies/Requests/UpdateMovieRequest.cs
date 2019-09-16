using BlackSlope.Api.Operations.Movies.ViewModels;

namespace BlackSlope.Api.Operations.Movies.Requests
{
    public class UpdateMovieRequest
    {
        public int? Id { get; set; }

        public MovieViewModel Movie { get; set; }
    }
}
