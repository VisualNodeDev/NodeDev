using NodeDev.Core.Connections;

namespace NodeDev.Core.Nodes
{
	public abstract class NoFlowNode : Node
	{
		public override string TitleColor => "lightgreen";

		public override string GetExecOutputPathId(string pathId, Connection execOutput)
		{
			throw new NotImplementedException();
		}

		public override bool DoesOutputPathAllowDeadEnd(Connection execOutput) => throw new NotImplementedException();

		public override bool DoesOutputPathAllowMerge(Connection execOutput) => throw new NotImplementedException();

		public NoFlowNode(Graph graph, string? id = null) : base(graph, id)
		{
		}

		public override bool IsFlowNode => false;
	}
}
