Feature: Class and Method Management
	Test class and method creation, renaming, and deletion

Scenario: Create a new class
	Given I load the default project
	When I create a new class named 'TestClass'
	Then The 'TestClass' should appear in the project explorer
	And I take a screenshot named 'new-class-created'

Scenario: Rename an existing class
	Given I load the default project
	When I click on the 'Program' class
	And I rename the class to 'MyProgram'
	Then The class should be named 'MyProgram' in the project explorer
	And I take a screenshot named 'class-renamed-success'

Scenario: Delete a class
	Given I load the default project
	When I create a new class named 'TempClass'
	And I delete the 'TempClass' class
	Then The 'TempClass' should not be in the project explorer
	And I take a screenshot named 'class-deleted'

Scenario: Add a new method to a class
	Given I load the default project
	When I click on the 'Program' class
	And I create a new method named 'TestMethod'
	Then The 'TestMethod' should appear in the method list
	And I take a screenshot named 'method-added'

Scenario: Rename a method
	Given I load the default project
	When I click on the 'Program' class
	And I rename the 'Main' method to 'MainProgram'
	Then The method should be named 'MainProgram'
	And I take a screenshot named 'method-renamed'

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

Scenario: Add class properties
	Given I load the default project
	When I click on the 'Program' class
	And I add a property named 'MyProperty' of type 'string'
	Then The property should appear in the class explorer
	And I take a screenshot named 'property-added'

Scenario: Test method visibility in class explorer
	Given I load the default project
	When I click on the 'Program' class
	Then All methods should be visible and not overlapping
	And Method names should be readable
	And I take a screenshot named 'methods-visibility-check'
