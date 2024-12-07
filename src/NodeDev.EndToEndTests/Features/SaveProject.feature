Feature: Save a project to file system

Scenario: Save empty project
	Given I load the default project
	Then The 'Main' method in the 'Program' class should exist
    
	Given I save the current project as 'EmptyProject'
	Then Snackbar should contain 'Project saved'