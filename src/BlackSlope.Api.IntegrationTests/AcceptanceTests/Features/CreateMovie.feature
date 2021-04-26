Feature: CreateMovie
	
	Use this operation to create a new movie and provide the following parameters 

	1. Title 
	2. Description
	3. ReleaseDate

@CreateMovie
Scenario: Add a new movie the the database 
	Given a user creates a new movie using post movie endpoint 
	And the movie is successfully created   