Feature: Node Manipulation and Connections
	Test drag-and-drop of nodes and creating connections between nodes

Scenario: Move a Return node on the canvas
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I drag the 'Return' node by 200 pixels to the right and 100 pixels down
	Then The 'Return' node should have moved from its original position

Scenario: Move Return node multiple times
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I drag the 'Return' node by 150 pixels to the right and 80 pixels down
	Then The 'Return' node should have moved from its original position
	When I drag the 'Return' node by 150 pixels to the right and 80 pixels down
	Then The 'Return' node should have moved from its original position
	When I drag the 'Return' node by -200 pixels to the right and -100 pixels down
	Then The 'Return' node should have moved from its original position

Scenario: Create connection between Entry and Return nodes
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I move the 'Return' node away from 'Entry' node
	And I take a screenshot named 'nodes-separated'
	When I connect the 'Entry' 'Exec' output to the 'Return' 'Exec' input
	Then I take a screenshot named 'after-connection'

Scenario: Disconnect and reconnect nodes
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I take a screenshot named 'initial-connection'
	And I move the 'Return' node away from 'Entry' node
	Then I take a screenshot named 'after-move'
	When I connect the 'Entry' 'Exec' output to the 'Return' 'Exec' input
	Then I take a screenshot named 'reconnected'

Scenario: Delete connection between Entry and Return nodes
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I delete the connection between 'Entry' 'Exec' output and 'Return' 'Exec' input
	Then There should be no console errors
	And I take a screenshot named 'connection-deleted'

Scenario: Open method and check for browser errors
	Given I load the default project
	When I check for console errors
	And I open the 'Main' method in the 'Program' class
	Then There should be no console errors
	And The graph canvas should be visible
