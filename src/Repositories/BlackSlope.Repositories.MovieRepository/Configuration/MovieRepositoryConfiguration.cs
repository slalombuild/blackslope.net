namespace BlackSlope.Repositories.MovieRepository.Configuration
{
    public class MovieRepositoryConfiguration : IMovieRepositoryConfiguration
    {
        public string MoviesConnectionString { get; set; }
    }
}