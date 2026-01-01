Feature: Node Manipulation and Connections
	Test drag-and-drop of nodes and creating connections between nodes

Scenario: Move a Return node on the canvas
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I drag the 'Return' node by 200 pixels to the right and 100 pixels down
	Then The 'Return' node should have moved from its original position
