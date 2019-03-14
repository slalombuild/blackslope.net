using System.Collections.Generic;
using AutoMapper;
using BlackSlope.Hosts.Api.Common.Controllers;
using BlackSlope.Hosts.Api.Operations.Movies.Requests;
using BlackSlope.Hosts.Api.Operations.Movies.Responses;
using BlackSlope.Hosts.Api.Operations.Movies.Validators.Interfaces;
using BlackSlope.Hosts.Api.Operations.Movies.ViewModels;
using BlackSlope.Services.MovieService;
using BlackSlope.Services.MovieService.DomainModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BlackSlope.Hosts.Api.Operations.Movies
{    
    // todo: enable this once authentication middleware has been configured
    //[Authorize]
    public class MoviesController : BaseController
    {
        private readonly IMapper _mapper;
        private readonly IMovieService _movieService;
        private readonly ICreateMovieRequestValidator _createMovieRequestValidator;
        private readonly IUpdateMovieRequestValidator _updateMovieRequestValidator;


        public MoviesController(IMovieService movieService, IMapper mapper, ICreateMovieRequestValidator createMovieRequestValidator, IUpdateMovieRequestValidator updateMovieRequestValidator)
        {
            _mapper = mapper;
            _movieService = movieService;
            _createMovieRequestValidator = createMovieRequestValidator;
            _updateMovieRequestValidator = updateMovieRequestValidator;
        }

        /// <summary>
        /// Return a list of all movies
        /// </summary>
        /// <remarks>
        /// Use this operation to return a list of all movies
        /// </remarks>
        /// <response code="200">Returns a list of all movies</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Internal Server Error</response>  
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet]
        [Route("api/v1/movies")]
        public ActionResult<GetMoviesResponse> Get()
        {
            // get all movies form service
            var movies = _movieService.GetAllMovies();

            // prepare response
            var response = new GetMoviesResponse
            {
                Movies = _mapper.Map<List<MovieResponseViewModel>>(movies)
            };

            // 200 response
            return HandleSuccessResponse(response);
        }

        /// <summary>
        /// Return a single movies
        /// </summary>
        /// <remarks>
        /// Use this operation to return a single movies
        /// </remarks>
        /// <response code="200">Returns a movie</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Internal Server Error</response>     
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet]
        [Route("api/v1/movies/{id}")]
        public ActionResult<GetMovieResponse> Get(int id)
        {
            // get all movies form service
            var movie = _movieService.GetMovie(id);

            // prepare response
            var response = new GetMovieResponse
            {
                Movie = _mapper.Map<MovieResponseViewModel>(movie)
            };

            // 200 response
            return HandleSuccessResponse(response);
        }

        /// <summary>
        /// Create a new movie
        /// </summary>
        /// <remarks>
        /// Use this operation to create a new movie
        /// </remarks>
        /// <response code="201">Movie successfully created, will return the new movie</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost]
        [Route("api/v1/movies")]
        public ActionResult<CreateMovieResponse> Post([FromBody] CreateMovieRequest request)
        {
            // validate request model           
            _createMovieRequestValidator.Validate(request);

            // map view model to domain model
            var movie = _mapper.Map<MovieDomainModel>(request.Movie);

            // create new movie
            var createdMovie = _movieService.CreateMovie(movie);

            // prepare response
            var response = new CreateMovieResponse
            {
                Movie = _mapper.Map<MovieResponseViewModel>(createdMovie)
            };

            // 201 response
            return HandleCreatedResponse(response);
        }

        /// <summary>
        /// Update an existing movie
        /// </summary>
        /// <remarks>
        /// Use this operation to update an existing movie
        /// </remarks>
        /// <response code="200">Movie successfully updated, will return the updated movie</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Internal Server Error</response>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut]
        [Route("api/v1/movies")]
        public ActionResult<UpdateMovieResponse> Put([FromBody] UpdateMovieRequest request)
        {
            // validate request model           
            _updateMovieRequestValidator.Validate(request);

            // map view model to domain model
            var movie = _mapper.Map<MovieDomainModel>(request.Movie);

            // update existing movie
            var updatedMovie = _movieService.UpdateMovie(movie);

            // prepare response
            var response = new UpdateMovieResponse
            {
                Movie = _mapper.Map<MovieResponseViewModel>(updatedMovie)
            };

            // 200 response
            return HandleSuccessResponse(response);
        }

        /// <summary>
        /// Delete an existing movie
        /// </summary>
        /// <remarks>
        /// Use this operation to delete an existing movie
        /// </remarks>
        /// <response code="200">Movie successfully delete, will return deleted Movie Id</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Internal Server Error</response>
        /// 
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpDelete]
        [Route("api/v1/movies/{id}")]
        public ActionResult<DeletedMovieResponse> Delete(int id)
        {
            // delete existing movie
            var deletedMovieId = _movieService.DeleteMovie(id);

            // prepare response
            var response = new DeletedMovieResponse
            {
                DeletedMovieId = deletedMovieId
            };

            // 200 response
            return HandleSuccessResponse(response);
        }
    }
}
