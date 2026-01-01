Feature: Project Management
	Test project creation, saving, loading, and management

Scenario: Create a new empty project
	When I create a new project
	Then A new project should be created with default class
	And I take a screenshot named 'new-project-created'

Scenario: Save project with custom name
	Given I load the default project
	When I save the current project as 'MyCustomProject'
	Then Snackbar should contain 'Project saved'
	And The project file should exist
	And I take a screenshot named 'project-saved'

Scenario: Save project after modifications
	Given I load the default project
	When I create a new class named 'ModifiedClass'
	And I save the current project as 'ModifiedProject'
	Then The modifications should be saved
	And Snackbar should contain 'Project saved'
	And I take a screenshot named 'modified-project-saved'

Scenario: Project export functionality
	Given I load the default project
	When I export the project
	Then The project should be exported successfully
	And Export files should be created
	And I take a screenshot named 'project-exported'

Scenario: Build project from UI
	Given I load the default project
	When I click the build button
	Then The project should compile successfully
	And Build output should be displayed
	And I take a screenshot named 'project-built'

Scenario: Run project from UI
	Given I load the default project with executable
	When I click the run button
	Then The project should execute
	And Output should be displayed
	And I take a screenshot named 'project-running'

Scenario: View project settings
	Given I load the default project
	When I open project settings
	Then Settings panel should appear
	And All settings should be editable
	And I take a screenshot named 'project-settings'


