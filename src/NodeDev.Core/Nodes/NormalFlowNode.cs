﻿using NodeDev.Core.Connections;
using NodeDev.Core.Types;

namespace NodeDev.Core.Nodes
{
	/// <summary>
	/// Nodes that want to implement a normal flow should inherit from this class.
	/// It'll automatically add a input exec and output exec.
	/// </summary>
	public abstract class NormalFlowNode : Node
	{
		public override string TitleColor => "lightblue";

		public override bool IsFlowNode => true;

		public override string GetExecOutputPathId(string pathId, Connection execOutput)
		{
			if (execOutput != Outputs[0])
				throw new InvalidOperationException("Invalid exec output connection.");

			return pathId; // no need to change the pathId, we're just flowing through like a->b->c
		}

		public override bool DoesOutputPathAllowDeadEnd(Connection execOutput) => false; // A normal flow node should not allow dead ends, unless a parent allowed it.

		public override bool DoesOutputPathAllowMerge(Connection execOutput) => throw new NotImplementedException(); // Since there is only one exec, it doesn't make sense to talk about merging, nothing to merge after all.

		protected NormalFlowNode(Graph graph, string? id = null) : base(graph, id)
		{
			Inputs.Add(new("Exec", this, TypeFactory.ExecType));
			Outputs.Add(new("Exec", this, TypeFactory.ExecType));
		}
	}
}
