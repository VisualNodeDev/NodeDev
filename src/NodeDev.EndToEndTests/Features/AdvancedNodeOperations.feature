Feature: Advanced Node Operations
	Test advanced node manipulation scenarios

Scenario: Add multiple nodes and connect them
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I add a 'DeclareVariable' node to the canvas
	And I add an 'Add' node to the canvas
	And I connect nodes together
	Then All nodes should be properly connected
	And I take a screenshot named 'multiple-nodes-connected'

Scenario: Search and add specific node types
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I search for 'Branch' nodes
	And I add a 'Branch' node from search results
	Then The 'Branch' node should be visible on canvas
	And I take a screenshot named 'branch-node-added'

Scenario: Move multiple nodes at once
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I select multiple nodes
	And I move the selected nodes by 150 pixels right
	Then All selected nodes should have moved
	And I take a screenshot named 'multi-node-move'

Scenario: Delete multiple connections
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I create multiple connections between nodes
	And I delete all connections from 'Entry' node
	Then The 'Entry' node should have no connections
	And I take a screenshot named 'connections-deleted'

Scenario: Test undo/redo functionality
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I add a 'DeclareVariable' node to the canvas
	And I undo the last action
	Then The 'DeclareVariable' node should not be visible
	When I redo the last action
	Then The 'DeclareVariable' node should be visible
	And I take a screenshot named 'undo-redo-test'

Scenario: Copy and paste nodes
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I select the 'Return' node
	And I copy the selected node
	And I paste the node
	Then There should be two 'Return' nodes on the canvas
	And I take a screenshot named 'node-copied'

Scenario: Test node properties panel
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I click on a 'Return' node
	Then The node properties panel should appear
	And The properties should be editable
	And I take a screenshot named 'node-properties'

Scenario: Test connection port colors
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I hover over a port
	Then The port should highlight
	And The port color should indicate its type
	And I take a screenshot named 'port-hover-highlight'

Scenario: Test zoom and pan operations
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I zoom in on the canvas
	Then The canvas should be zoomed in
	When I zoom out on the canvas
	Then The canvas should be zoomed out
	When I pan the canvas
	Then The canvas view should have moved
	And I take a screenshot named 'zoom-pan-operations'

Scenario: Test canvas reset and fit
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I move nodes far from origin
	And I reset canvas view
	Then All nodes should be centered
	And I take a screenshot named 'canvas-reset'
