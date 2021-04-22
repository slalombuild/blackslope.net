Feature: DeleteMovie
	

	   Use this operation to delete a new movie and provide the following parameters 

	1. movie id 


@DeleteMovie
Scenario: Delete an existing movie
	Given a user creates a new movie using post movie endpoint 
	And the movie is successfully created  
	Given a user gets the movie id of recently created movie 
	Given a deletes a recently created movie
	And the movie is successfully deleted  