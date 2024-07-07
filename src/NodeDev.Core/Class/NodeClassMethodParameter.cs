using NodeDev.Core.Connections;
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
        private record class SerializedNodeClassMethodParameter(string Name, string ParameterType, bool? IsOut);

        public string Name { get; private set; }

        public TypeBase ParameterType { get; private set; }

        public NodeClassMethod Method { get; }

        public bool IsOut { get; set; }

        public NodeClassMethodParameter(string name, TypeBase parameterType, NodeClassMethod method)
        {
            Name = name;
            ParameterType = parameterType;
            Method = method;
        }

        public string Serialize()
        {
            return System.Text.Json.JsonSerializer.Serialize(new SerializedNodeClassMethodParameter(Name, ParameterType.SerializeWithFullTypeName(), IsOut));
        }

        public static NodeClassMethodParameter Deserialize(TypeFactory typeFactory, string serialized, NodeClassMethod nodeClassMethod)
        {
            var serializedNodeClassMethodParameter = System.Text.Json.JsonSerializer.Deserialize<SerializedNodeClassMethodParameter>(serialized) ?? throw new Exception("Unable to deserialize node class method parameter");
            return new NodeClassMethodParameter(serializedNodeClassMethodParameter.Name, TypeBase.Deserialize(typeFactory, serializedNodeClassMethodParameter.ParameterType), nodeClassMethod)
            {
                IsOut = serializedNodeClassMethodParameter.IsOut ?? false
            };
        }

        #region Actions from UI

        public void SetIsOut(bool value)
        {
            if (value == IsOut)
                return;

            IsOut = value;

            RefreshAllMethodCalls();
            RefreshEntryAndReturnNodes();
        }

        private void RefreshEntryAndReturnNodes()
        {
            var entry = Method.Graph.Nodes.Values.OfType<EntryNode>().First();
            var returnNodes = Method.Graph.Nodes.Values.OfType<ReturnNode>().ToList();

            entry.Refresh();
            foreach (var returnNode in returnNodes)
                returnNode.Refresh();
        }

        private void RefreshAllMethodCalls()
        {
            foreach (var methodCall in Method.Class.Project.GetNodes<MethodCall>())
            {
                if (methodCall.TargetMethod == Method)
                {
                    methodCall.SelectOverload(((IMethodInfo)Method).AlternateOverload(), out var newConnections, out var removedConnections);

                    methodCall.Graph.MergedRemovedConnectionsWithNewConnections(newConnections, removedConnections);
                }
            }
        }

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

            RefreshAllMethodCalls();
            RefreshEntryAndReturnNodes();
        }

        private void SwapParameter(int index, int newIndex)
        {
            var previous = Method.Parameters[newIndex];
            Method.Parameters[newIndex] = this;
            Method.Parameters[index] = previous;

            RefreshAllMethodCalls();
            RefreshEntryAndReturnNodes();
        }

        public void Rename(string name)
        {
            var oldName = Name;
            Name = name;

            foreach (var methodCall in Method.Graph.Project.GetNodes<MethodCall>())
            {
                if (methodCall.TargetMethod == Method)
                    methodCall.OnMethodParameterRenamed(oldName, this);
            }

            var entry = Method.Graph.Nodes.Values.OfType<EntryNode>().FirstOrDefault();
            if (entry != null)
                entry.RenameParameter(this, Method.Parameters.IndexOf(this));
        }

        public void ChangeType(TypeBase type)
        {
            ParameterType = type;

            RefreshAllMethodCalls();
            RefreshEntryAndReturnNodes();
        }

        #endregion
    }
}
