Feature: UI Responsiveness and Error Handling
	Test UI responsiveness, error handling, and edge cases

Scenario: Test rapid node additions
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I rapidly add 10 nodes to the canvas
	Then All nodes should be added without errors
	And There should be no console errors
	And I take a screenshot named 'rapid-node-addition'

Scenario: Test large graph performance
	Given I load the default project with large graph
	When I open the method with many nodes
	Then The canvas should render without lag
	And All nodes should be visible
	And I take a screenshot named 'large-graph-loaded'

Scenario: Test invalid connection attempts
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I try to connect incompatible ports
	Then The connection should be rejected
	And An error message should appear
	And I take a screenshot named 'invalid-connection-rejected'

Scenario: Test deleting node with connections
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I delete a node that has connections
	Then The node and its connections should be removed
	And No orphaned connections should remain
	And I take a screenshot named 'node-with-connections-deleted'

Scenario: Test browser window resize
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I resize the browser window
	Then The UI should adapt to the new size
	And All elements should remain accessible
	And I take a screenshot named 'window-resized'

Scenario: Test keyboard shortcuts
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I use keyboard shortcut for delete
	Then The selected node should be deleted
	When I use keyboard shortcut for save
	Then The project should be saved
	And I take a screenshot named 'keyboard-shortcuts-work'

Scenario: Test long method names display
	Given I load the default project
	When I click on the 'Program' class
	And I create a method with a very long name
	Then The method name should display correctly without overflow
	And I take a screenshot named 'long-method-name'

Scenario: Test special characters in names
	Given I load the default project
	When I try to create a class with special characters
	Then Invalid characters should be rejected or sanitized
	And I take a screenshot named 'special-chars-handling'

Scenario: Test concurrent operations
	Given I load the default project
	And I open the 'Main' method in the 'Program' class
	When I perform multiple operations quickly
	Then All operations should complete successfully
	And There should be no race conditions
	And I take a screenshot named 'concurrent-operations'

Scenario: Test memory cleanup
	Given I load the default project
	When I open and close multiple methods repeatedly
	Then Memory usage should remain stable
	And There should be no memory leaks
	And I take a screenshot named 'memory-stable'
