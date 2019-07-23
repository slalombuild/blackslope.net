using System;

namespace BlackSlope.Hosts.Api.Operations.Movies.ViewModels
{
    public class CreateMovieRequestViewModel
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? ReleaseDate { get; set; }
    }
}
