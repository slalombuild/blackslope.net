using System;

namespace BlackSlope.Hosts.Api.Operations.Movies.ViewModels
{
    public class UpdateMovieRequestViewModel
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime? ReleaseDate { get; set; }
    }
}
