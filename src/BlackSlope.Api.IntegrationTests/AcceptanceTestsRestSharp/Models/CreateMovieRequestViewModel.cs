using System;

namespace AcceptanceTestsRestSharp.Models
{
    public  class CreateMovieRequestViewModel
    {
        public int? Id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public DateTime? ReleaseDate { get; set; }
    }
}
