Feature: Comprehensive UI Testing
	Test all UI functionality to ensure everything works correctly

Scenario: Test method listing and text display
	Given I load the default project
	When I click on the 'Program' class
	Then I should see the 'Main' method listed
	And The method text should be readable without overlap
	And I take a screenshot named 'method-list-display'

Scenario: Test deleting connections
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I take a screenshot named 'before-disconnect'
	And I disconnect the 'Entry' 'Exec' from 'Return' 'Exec'
	Then I take a screenshot named 'after-disconnect'
	And The connection should be removed

Scenario: Test opening multiple methods
	Given I load the default project
	When I click on the 'Program' class
	And I open the 'Main' method in the 'Program' class
	Then The graph canvas should be visible
	When I go back to class explorer
	And I open the 'Main' method in the 'Program' class again
	Then The graph canvas should still be visible
	And I take a screenshot named 'multiple-method-opens'

Scenario: Test switching between classes
	Given I load the default project
	When I click on the 'Program' class
	And I take a screenshot named 'program-class-view'
	When I click on a different class if available
	Then The class explorer should update
	And I take a screenshot named 'switched-class-view'

Scenario: Test console errors during all operations
	Given I load the default project
	When I check for console errors
	And I click on the 'Program' class
	And I open the 'Main' method in the 'Program' class
	And I drag the 'Return' node by 100 pixels to the right and 50 pixels down
	Then There should be no console errors
	And I take a screenshot named 'operations-no-errors'
