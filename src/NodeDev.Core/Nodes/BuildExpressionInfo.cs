using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes;

internal class BuildExpressionInfo
{
	public BuildExpressionInfo(LabelTarget returnLabel, BuildExpressionOptions buildExpressionOptions, ParameterExpression? thisExpression)
	{
		ReturnLabel = returnLabel;
		BuildExpressionOptions = buildExpressionOptions;
		ThisExpression = thisExpression;
	}

	public Dictionary<string, ParameterExpression> MethodParametersExpression { get; } = [];

	public Dictionary<Connection, Expression> LocalVariables { get; } = [];

	public LabelTarget ReturnLabel { get; }

	public BuildExpressionOptions BuildExpressionOptions { get; }

	/// <summary>
	/// Represent 'this', if the method being built is not static
	/// </summary>
	public ParameterExpression? ThisExpression { get; }

	/// <summary>
	/// Used to track which nodes was already inlined.
	/// There is no need to calculate the outputs of inlined nodes multiple times even if they are referenced many times.
	/// </summary>
	public HashSet<Node> InlinedNodes { get; } = [];
}
