Feature: UpdateMoviebyId
  Use this operation to get information of a movie and update the info

    1. movie id 


@UpdateMoviebyUd
Scenario: Fetch an existing movie info by id to update
	Given a user creates a new movie using post movie endpoint 
	And the movie is successfully created   
    Given a user gets the movie id of recently created movie 
    And the user gets movie information using get movie by id endpoint
	And the get movie by id response is successful  
    Given a user updates the information of recently created movie with the following info 
	And the update movie by id response is successful 