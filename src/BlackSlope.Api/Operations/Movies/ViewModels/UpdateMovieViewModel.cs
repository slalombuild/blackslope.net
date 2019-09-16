using System;

namespace BlackSlope.Api.Operations.Movies.ViewModels
{
    public class UpdateMovieViewModel
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime? ReleaseDate { get; set; }
    }
}
