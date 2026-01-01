Feature: Console Output
	Test that console output from WriteLine nodes appears in the bottom panel when running the project

Scenario: Console panel shows output when running project with WriteLine
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I run the project
	Then The console panel should become visible
	And I wait for the project to complete

