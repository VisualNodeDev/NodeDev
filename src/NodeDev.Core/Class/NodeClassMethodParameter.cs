using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Flow;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Class
{
	public class NodeClassMethodParameter : IMethodParameterInfo
	{
		private record class SerializedNodeClassMethodParameter(string Name, string ParameterTypeFullName, string ParameterType);

		public string Name { get; private set; }

		public TypeBase ParameterType { get; private set; }

		public NodeClassMethod Method { get; }

		public NodeClassMethodParameter(string name, TypeBase parameterType, NodeClassMethod method)
		{
			Name = name;
			ParameterType = parameterType;
			Method = method;
		}

		public string Serialize()
		{
			return System.Text.Json.JsonSerializer.Serialize(new SerializedNodeClassMethodParameter(Name, ParameterType.GetType().FullName!, ParameterType.Serialize()));
		}

		public static NodeClassMethodParameter Deserialize(TypeFactory typeFactory, string serialized, NodeClassMethod nodeClassMethod)
		{
			var serializedNodeClassMethodParameter = System.Text.Json.JsonSerializer.Deserialize<SerializedNodeClassMethodParameter>(serialized) ?? throw new Exception("Unable to deserialize node class method parameter");
			return new NodeClassMethodParameter(serializedNodeClassMethodParameter.Name, TypeBase.Deserialize(typeFactory, serializedNodeClassMethodParameter.ParameterTypeFullName, serializedNodeClassMethodParameter.ParameterType), nodeClassMethod);
		}

		#region Actions from UI

		public void MoveUp()
		{
			var index = Method.Parameters.IndexOf(this);
			if (index <= 0)
				return;

			SwapParameter(index, index - 1);
		}

		public void MoveDown()
		{
			var index = Method.Parameters.IndexOf(this);
			if (index == Method.Parameters.Count - 1 || index == -1)
				return;

			SwapParameter(index, index + 1);
		}

		public void Remove()
		{
			var index = Method.Parameters.IndexOf(this);
			if (index == -1)
				return;

			Method.Parameters.RemoveAt(index);

			foreach (var methodCall in Method.Class.Project.GetNodes<MethodCall>())
			{
				if (methodCall.TargetMethod == Method)
				{
					methodCall.RemoveParameterAt(index);

					Method.Class.Project.GraphChangedSubject.OnNext(methodCall.Graph);
				}
			}

			var entry = Method.Graph.Nodes.Values.OfType<EntryNode>().FirstOrDefault();
			if (entry != null)
			{
				entry.RemoveParameterAt(index);
				Method.Class.Project.GraphChangedSubject.OnNext(Method.Graph);
			}
		}

		private void SwapParameter(int index, int newIndex)
		{
			var previous = Method.Parameters[newIndex];
			Method.Parameters[newIndex] = this;
			Method.Parameters[index] = previous;

			foreach (var methodCall in Method.Class.Project.GetNodes<MethodCall>())
			{
				if (methodCall.TargetMethod == Method)
				{
					methodCall.SwapParameter(newIndex, index);

					Method.Class.Project.GraphChangedSubject.OnNext(methodCall.Graph);
				}
			}

			var entry = Method.Graph.Nodes.Values.OfType<EntryNode>().FirstOrDefault();
			if (entry != null)
			{
				entry.SwapParameter(newIndex, index);
				Method.Class.Project.GraphChangedSubject.OnNext(Method.Graph);
			}
		}

		public void Rename(string name)
		{
			Name = name;

			foreach (var nodeClass in Method.Class.Project.Classes)
			{
				foreach (var method in nodeClass.Methods)
				{
					bool hasChanged = false;
					foreach (var node in method.Graph.Nodes.Values)
					{
						if (node is MethodCall methodCall && methodCall.TargetMethod == Method)
						{
							hasChanged = true;

							methodCall.OnMethodParameterRenamed(this);
						}
					}

					if (hasChanged)
						Method.Class.Project.GraphChangedSubject.OnNext(method.Graph);
				}
			}

			var entry = Method.Graph.Nodes.Values.OfType<EntryNode>().FirstOrDefault();
			if (entry != null)
			{
				entry.RenameParameter(this, Method.Parameters.IndexOf(this));
				Method.Class.Project.GraphChangedSubject.OnNext(Method.Graph);
			}
		}

		public void ChangeType(TypeBase type)
		{
			ParameterType = type;

			foreach (var nodeClass in Method.Class.Project.Classes)
			{
				foreach (var method in nodeClass.Methods)
				{
					bool hasChanged = false;
					foreach (var node in method.Graph.Nodes.Values)
					{
						if (node is MethodCall methodCall && methodCall.TargetMethod == Method)
						{
							hasChanged = true;

							var connection = methodCall.OnMethodParameterTypeChanged(this);

							// there's a bug in the UI, the connection isn't remove even though the following code seemed to work : 
							//foreach(var otherConnection in connection.Connections.ToList())
							//{
							//    if (!otherConnection.Type.IsAssignableTo(type))
							//        method.Graph.Disconnect(connection, otherConnection);
							//}
						}
					}

					if (hasChanged)
						Method.Class.Project.GraphChangedSubject.OnNext(method.Graph);
				}
			}

			var entry = Method.Graph.Nodes.Values.OfType<EntryNode>().FirstOrDefault();
			if (entry != null)
			{
				var connection = entry.UpdateParameterType(this, Method.Parameters.IndexOf(this));
				// there's a bug in the UI, the connection isn't remove even though the following code seemed to work : 
				//foreach(var otherConnection in connection.Connections.ToList())
				//{
				//    if (!otherConnection.Type.IsAssignableTo(type))
				//        method.Graph.Disconnect(connection, otherConnection);
				//}

				Method.Class.Project.GraphChangedSubject.OnNext(Method.Graph);
			}
		}

		#endregion
	}
}
