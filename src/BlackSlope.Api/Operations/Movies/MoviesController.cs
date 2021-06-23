using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Threading.Tasks;
using AutoMapper;
using BlackSlope.Api.Common.Controllers;
using BlackSlope.Api.Common.Exceptions;
using BlackSlope.Api.Common.Validators.Interfaces;
using BlackSlope.Api.Operations.Movies.Requests;
using BlackSlope.Api.Operations.Movies.Responses;
using BlackSlope.Api.Operations.Movies.ViewModels;
using BlackSlope.Services.Movies;
using BlackSlope.Services.Movies.DomainModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BlackSlope.Api.Operations.Movies
{
    // TODO: enable this once authentication middleware has been configured
    // [Authorize]
    public class MoviesController : BaseController
    {
        private readonly IMapper _mapper;
        private readonly IMovieService _movieService;
        private readonly IBlackSlopeValidator _blackSlopeValidator;

        public MoviesController(IMovieService movieService, IMapper mapper, IBlackSlopeValidator blackSlopeValidator)
        {
            _mapper = mapper;
            _blackSlopeValidator = blackSlopeValidator;
            _movieService = movieService;
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
        public async Task<ActionResult<List<MovieViewModel>>> Get()
        {
            // get all movies form service
            var movies = await _movieService.GetAllMoviesAsync();

            // prepare response
            var response = _mapper.Map<List<MovieViewModel>>(movies);

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
        public async Task<ActionResult<MovieViewModel>> Get(int id)
        {
            // get all movies form service
            var movie = await _movieService.GetMovieAsync(id);

            // prepare response
            var response = _mapper.Map<MovieViewModel>(movie);

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
        public async Task<ActionResult<MovieViewModel>> Post([FromBody] CreateMovieViewModel viewModel)
        {
            var request = new CreateMovieRequest { Movie = viewModel };

            // validate request model
            await _blackSlopeValidator.AssertValidAsync(request);

            // map view model to domain model
            var movie = _mapper.Map<MovieDomainModel>(viewModel);

            // create new movie
            var createdMovie = await _movieService.CreateMovieAsync(movie);

            // prepare response
            var response = _mapper.Map<MovieViewModel>(createdMovie);

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
        [Route("api/v1/movies/{id}")]
        public async Task<ActionResult<MovieViewModel>> Put(int? id, [FromBody] MovieViewModel viewModel)
        {
            Contract.Requires(viewModel != null);
            var request = new UpdateMovieRequest { Movie = viewModel, Id = id };

            await _blackSlopeValidator.AssertValidAsync(request);

            // id can be in URL, body, or both
            viewModel.Id = id ?? viewModel.Id;

            // map view model to domain model
            var movie = _mapper.Map<MovieDomainModel>(viewModel);

            // update existing movie
            var updatedMovie = await _movieService.UpdateMovieAsync(movie);

            // prepare response
            var response = _mapper.Map<MovieViewModel>(updatedMovie);

            // 200 response
            return HandleSuccessResponse(response);
        }

        /// <summary>
        /// Delete an existing movie
        /// </summary>
        /// <remarks>
        /// Use this operation to delete an existing movie
        /// </remarks>
        /// <response code="204">Movie successfully delete, no content</response>
        /// <response code="400">Bad Request</response>
        /// <response code="401">Unauthorized</response>
        /// <response code="500">Internal Server Error</response>
        ///
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpDelete]
        [Route("api/v1/movies/{id}")]
        public async Task<ActionResult<DeletedMovieResponse>> Delete(int id)
        {
            // delete existing movie
            await _movieService.DeleteMovieAsync(id);

            // 204 response
            return HandleDeletedResponse();
        }


        /// <summary>
        /// This is a sample error.
        /// </summary>
        /// <remarks>
        /// Use this operation to test the global error handling
        /// </remarks>
        [HttpGet]
        [Route("SampleError")]
        public object SampleError()
        {
            throw new HandledException(ExceptionType.Security, "This is an example security issue.", System.Net.HttpStatusCode.RequestEntityTooLarge);
        }
    }
}
