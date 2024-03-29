﻿using NodeDev.Core.Class;
using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes
{
	public class New : NormalFlowNode
	{
		public override string Name
		{
			get => Outputs[1].Type.HasUndefinedGenerics ? "New ?" : $"New {Outputs[1].Type.FriendlyName}";
			set { }
		}

		public New(Graph graph, string? id = null) : base(graph, id)
		{
			Outputs.Add(new("Obj", this, new UndefinedGenericType("T")));
		}

		public override IEnumerable<AlternateOverload> AlternatesOverloads
		{
			get
			{
				if (Outputs[1].Type is UndefinedGenericType)
					return Enumerable.Empty<AlternateOverload>();

				if (Outputs[1].Type is RealType realType)
				{
					var constructors = realType.BackendType.GetConstructors();
					return constructors.Select(x => new AlternateOverload(Outputs[1].Type, x.GetParameters().Select(y => new RealMethodParameterInfo(y, TypeFactory, realType)).OfType<IMethodParameterInfo>().ToList())).ToList();
				}
				else if (Outputs[1].Type is NodeClassType nodeClassType)
					return new AlternateOverload[] { new(Outputs[1].Type, new()) }; // for now, we don't handle custom constructors

				else
					throw new Exception("Unknowned type in New node: " + Outputs[1].Type.Name);
			}
		}
		public override List<Connection> GenericConnectionTypeDefined(UndefinedGenericType previousType, Connection connection, TypeBase newType)
		{
			var constructor = AlternatesOverloads.First();

			Inputs.AddRange(constructor.Parameters.Select(x => new Connection(x.Name ?? "??", this, x.ParameterType)));

			return new();
		}

		public override void SelectOverload(AlternateOverload overload, out List<Connection> newConnections, out List<Connection> removedConnections)
		{
			removedConnections = Inputs.Skip(1).ToList();
			Inputs.RemoveRange(1, Inputs.Count - 1);

			newConnections = overload.Parameters.Select(x => new Connection(x.Name, this, x.ParameterType)).ToList();
			Inputs.AddRange(newConnections);
		}

		protected override void ExecuteInternal(GraphExecutor executor, object? self, Span<object?> inputs, Span<object?> outputs, ref object? state)
		{
			if (Outputs[1].Type is UndefinedGenericType)
				throw new InvalidOperationException("Output type is not defined");

			if (Outputs[1].Type is RealType realType)
				outputs[1] = Activator.CreateInstance(realType.MakeRealType(), inputs[1..].ToArray());
			else if (Outputs[1].Type is NodeClassType nodeClassType)
				outputs[1] = Activator.CreateInstance(Graph.SelfClass.Project.GetCreatedClassType(nodeClassType.NodeClass), inputs[1..].ToArray());
			else
				throw new Exception("Unknown type:" + Outputs[1].Name);
		}
	}
}
