using System.ComponentModel;

namespace BlackSlope.Hosts.Api.Operations.Movies.Enumerators
{
    public enum MovieErrorCode
    {
        [Description("Request model cannot be null")]
        NullRequestModel = 40001,

        [Description("Movie Id cannot be null or empty")]
        EmptyOrNullMovieId = 40002,

        [Description("Movie Title cannot be null or empty")]
        EmptyOrNullMovieTitle = 40003,

        [Description("Movie Description cannot be null or empty")]
        EmptyOrNullMovieDescription = 40004,

        [Description("Movie Title should be between 2 and 50 characters")]
        TitleNotBetween2and50Characters = 40005,

        [Description("Movie Description should be between 2 and 50 characters")]
        DescriptionNotBetween2and50Characters = 40006

    }
}
