Feature: GetMoviebyId

  Use this operation to get information of a movie and provide the following parameters 

    1. movie id 


@GetMoviebyUd
Scenario: Fetch an existing movie info by id 
	Given a user creates a new movie using post movie endpoint 
	And the movie is successfully created   
    Given a user gets the movie id of recently created movie 
    And the user gets movie information using get movie by id endpoint
	And the get movie by id response is successful  