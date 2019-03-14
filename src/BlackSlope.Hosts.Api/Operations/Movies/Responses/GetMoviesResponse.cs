using BlackSlope.Hosts.Api.Operations.Movies.ViewModels;
using System.Collections.Generic;

namespace BlackSlope.Hosts.Api.Operations.Movies.Responses
{
    public class GetMoviesResponse
    {
        public IEnumerable<MovieResponseViewModel> Movies { get; set; }
    }
}