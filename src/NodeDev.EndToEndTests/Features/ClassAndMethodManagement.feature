Feature: Class and Method Management
	Test class and method creation, renaming, and deletion

Scenario: Add a new method to a class
	Given I load the default project
	When I click on the 'Program' class
	And I create a new method named 'TestMethod'
	Then The 'TestMethod' should appear in the method list
	And I take a screenshot named 'method-added'

Scenario: Delete a method
	Given I load the default project
	When I click on the 'Program' class
	And I create a new method named 'TempMethod'
	And I delete the 'TempMethod' method
	Then The 'TempMethod' should not be in the method list
	And I take a screenshot named 'method-deleted'

Scenario: Add method parameters
	Given I load the default project
	When I click on the 'Program' class
	And I open the 'Main' method in the 'Program' class
	And I add a parameter named 'testParam' of type 'int'
	Then The parameter should appear in the Entry node
	And I take a screenshot named 'parameter-added'

Scenario: Test method visibility in class explorer
	Given I load the default project
	When I click on the 'Program' class
	Then All methods should be visible and not overlapping
	And Method names should be readable
	And I take a screenshot named 'methods-visibility-check'
