
using NodeDev.Core.Connections;

namespace NodeDev.Core;

/// <summary>
/// Represent a list of paths in a graph.
/// Each path is saved as the list of every outputExec connection in the path.
/// </summary>
public class NodePaths
{
	public List<NodePath> Paths { get; } = [];

	public int CountPossiblePaths => Paths.Count;

	public NodePaths() { }

	public NodePaths(NodePath path)
	{
		Paths.Add(path);
	}

	/// <summary>
	/// Get a new NodePaths containing all the possible paths starting from the <paramref name="connection"/>.
	/// This will remove all the paths that didn't go through the <paramref name="connection"/>.
	/// </summary>
	public NodePaths GetPathsFromOutput(Connection connection)
	{
		var newPaths = new NodePaths();

		foreach (var path in Paths)
		{
			var newPath = path.CloneThenRemoveUntil(connection);

			// Check if the path is not null and if it's not already in the list
			if (newPath != null && !newPaths.HasSamePath(newPath))
				newPaths.Paths.Add(newPath);
		}

		return newPaths;
	}

	/// <summary>
	/// Get a new NodePaths containing all the possible paths leading to the <paramref name="connection"/>.
	/// The <paramref name="connection"/> is the last connection in each path.
	/// </summary>
	public NodePaths GetPathsLeadingToOutput(Connection connection)
	{
		var newPaths = new NodePaths();

		foreach (var path in Paths)
		{
			var newPath = path.CloneThenRemoveEverythingAfter(connection);

			// Check if the path is not null and if it's not already in the list
			if (newPath != null && !newPaths.HasSamePath(newPath))
				newPaths.Paths.Add(newPath);
		}

		return newPaths;

	}

	/// <summary>
	/// Check if the <paramref name="path"/> is contained in any of the paths.
	/// The path is contained if it is the exact same sequence of connections.
	/// </summary>
	public bool HasSamePath(NodePath path)
	{
		foreach (var localPath in Paths)
		{
			if (localPath.Connections.SequenceEqual(path.Connections))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Check if the <paramref name="connection"/> is contained in any of the paths.
	/// </summary>
	public bool Contains(Connection connection)
	{
		foreach (var path in Paths)
		{
			if (path.Contains(connection))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Add the paths to the current list of possible paths.
	/// </summary>
	/// <param name="path">Paths to add to the current list of possible paths</param>
	public void AddNewIndependantBranch(NodePaths path)
	{
		Paths.AddRange(path.Paths);
	}

	/// <summary>
	/// Add the path to the current list of possible paths.
	/// </summary>
	/// <param name="path">Path to add to the current list of possible paths</param>
	public void AddNewIndependantBranch(NodePath path)
	{
		Paths.Add(path);
	}

	/// <summary>
	/// Add the path at the end of each possible path.
	/// </summary>
	public void AppendPath(NodePath path)
	{
		foreach (var localPath in Paths)
			localPath.AppendPath(path);
	}

	/// <summary>
	/// Add the connection at the beginning of each possible path.
	/// </summary>
	public void PrependPath(NodePath path)
	{
		foreach (var localPath in Paths)
			localPath.Connections.InsertRange(0, path.Connections);
	}
}

public class NodePath
{
	public List<Connection> Connections { get; } = [];

	/// <summary>
	/// Get the length of the path. (Amount of connections in the path)
	/// </summary>
	public int Length => Connections.Count;

	public NodePath() { }

	public NodePath(IEnumerable<Connection> connections)
	{
		Connections.AddRange(connections);
	}

	public NodePath(List<Connection> connections)
	{
		Connections = connections;
	}

	/// <summary>
	/// Check if the <paramref name="connection"/> is contained in the path.
	/// </summary>
	public bool Contains(Connection connection)
	{
		return Connections.Contains(connection);
	}

	/// <summary>
	/// Create a clone of the path and crop the beginning up to the <paramref name="connection"/>.
	/// Return null if the connection is not in the path.
	/// </summary>
	public NodePath? CloneThenRemoveUntil(Connection connection)
	{
		var index = Connections.IndexOf(connection);
		if (index == -1)
			return null;

		var newPath = new NodePath(Connections[index..]);
		return newPath;
	}

	/// <summary>
	/// Create a clone of the path and crop the end after the <paramref name="connection"/>.
	/// </summary>
	/// <param name="connection"></param>
	/// <returns></returns>
	public NodePath? CloneThenRemoveEverythingAfter(Connection connection)
	{
		var index = Connections.IndexOf(connection);
		if (index == -1)
			return null;

		var newPath = new NodePath(Connections[..index]);
		return newPath;
	}

	/// <summary>
	/// Extend the current path by adding the <paramref name="paths"/> at the end of the path.
	/// </summary>
	/// <param name="path">Path to add to the end of this</param>
	public void AppendPath(NodePath path)
	{
		Connections.AddRange(path.Connections);
	}

	/// <summary>
	/// Extend the current path by adding the <paramref name="connection"/> at the end of the path.
	/// </summary>
	/// <param name="connection">Connection to add to this</param>
	public void AppendPath(Connection connection)
	{
		Connections.Add(connection);
	}
}
