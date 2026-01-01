Feature: Comprehensive UI Testing
	Test all UI functionality to ensure everything works correctly

Scenario: Test class operations
	Given I load the default project
	Then I should see the 'Program' class in the project explorer
	When I click on the 'Program' class
	Then The class explorer should show class details
	And I take a screenshot named 'class-selected'

Scenario: Test method listing and text display
	Given I load the default project
	When I click on the 'Program' class
	Then I should see the 'Main' method listed
	And The method text should be readable without overlap
	And I take a screenshot named 'method-list-display'

Scenario: Test renaming a class
	Given I load the default project
	When I click on the 'Program' class
	And I rename the class to 'TestProgram'
	Then The class should be named 'TestProgram'
	And I take a screenshot named 'class-renamed'

Scenario: Test adding and removing nodes
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I add a 'DeclareVariable' node to the canvas
	Then The 'DeclareVariable' node should be visible
	When I delete the 'DeclareVariable' node
	Then The 'DeclareVariable' node should not be visible
	And I take a screenshot named 'node-operations'

Scenario: Test deleting connections
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I take a screenshot named 'before-disconnect'
	And I disconnect the 'Entry' 'Exec' from 'Return' 'Exec'
	Then I take a screenshot named 'after-disconnect'
	And The connection should be removed

Scenario: Test generic type color changes
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I add a 'DeclareVariable' node to the canvas
	And I connect a generic type port
	Then The port color should change to reflect the type
	And I take a screenshot named 'generic-type-color'

Scenario: Test opening multiple methods
	Given I load the default project
	When I click on the 'Program' class
	And I open the 'Main' method in the 'Program' class
	Then The graph canvas should be visible
	When I go back to class explorer
	And I open the 'Main' method in the 'Program' class again
	Then The graph canvas should still be visible
	And I take a screenshot named 'multiple-method-opens'

Scenario: Test method name display integrity
	Given I load the default project
	When I click on the 'Program' class
	Then All method names should be displayed correctly
	And No text should overlap or appear corrupted
	And I take a screenshot named 'method-display-integrity'

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
