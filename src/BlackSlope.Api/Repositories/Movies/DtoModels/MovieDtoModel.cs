using System;

namespace BlackSlope.Repositories.Movies.DtoModels
{
    public class MovieDtoModel
    {
        public int Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime? ReleaseDate { get; set; }
    }
}
