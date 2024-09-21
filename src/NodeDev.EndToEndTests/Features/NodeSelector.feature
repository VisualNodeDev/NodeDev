Feature: Show node selection when dropping a node's connection

Background: 
	Given I load the default project
	Then Open the 'Main' method in the 'Program'
    
Scenario: Opening node selection from undefined generic type should show all results
	Given I add a node of type 'New' from connection 'Exec' of existing node 'ENTRY'

