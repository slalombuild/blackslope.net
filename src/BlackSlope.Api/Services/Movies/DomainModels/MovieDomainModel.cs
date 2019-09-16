using System;

namespace BlackSlope.Services.Movies.DomainModels
{
    public class MovieDomainModel
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime? ReleaseDate { get; set; }
    }
}
