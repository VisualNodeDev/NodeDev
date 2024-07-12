using NodeDev.Core.Nodes;
using NodeDev.Core.Nodes.Flow;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Class
{
    public class NodeClassMethod : IMethodInfo
    {
        internal record class SerializedNodeClassMethod(string Name, TypeBase.SerializedType ReturnType, List<NodeClassMethodParameter.SerializedNodeClassMethodParameter> Parameters, Graph.SerializedGraph Graph, bool IsStatic);
        public NodeClassMethod(NodeClass ownerClass, string name, TypeBase returnType, Graph graph, bool isStatic = false)
        {
            Class = ownerClass;
            Name = name;
            ReturnType = returnType;
            Graph = graph;
            IsStatic = isStatic;

            Graph.SelfMethod = this;
        }

        public NodeClass Class { get; }

        public string Name { get; private set; }

        public TypeBase ReturnType { get; }

        public List<NodeClassMethodParameter> Parameters { get; } = new();

        public Graph Graph { get; }

        public bool IsStatic { get; set; }

        public TypeBase DeclaringType => Class.ClassTypeBase;

        public bool HasReturnValue => ReturnType != Class.TypeFactory.Void;

        public void Rename(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName))
                return;

            Name = newName;

            Graph.RaiseGraphChanged(true);
        }

        public void AddDefaultParameter()
        {
            string name = "NewParameter";
            int i = 2;
            while (Parameters.Any(x => x.Name == name))
                name = $"NewParameter_{i++}";
            var newParameter = new NodeClassMethodParameter(name, Class.TypeFactory.Get<int>(), this);

            Parameters.Add(newParameter);

            foreach (var methodCall in Class.Project.GetNodes<MethodCall>())
            {
                if (methodCall.TargetMethod == this)
                    methodCall.OnNewMethodParameter(newParameter);
            }

            var entry = Graph.Nodes.Values.OfType<EntryNode>().FirstOrDefault();
            if (entry != null)
                entry.AddNewParameter(newParameter);
        }

        public IEnumerable<IMethodParameterInfo> GetParameters()
        {
            return Parameters;
        }

        public MethodInfo CreateMethodInfo()
        {
            var classType = Class.ClassTypeBase.MakeRealType();

            var method = classType.GetMethod(Name, GetParameters().Select(x => x.ParameterType.MakeRealType()).ToArray());

            if (method == null)
                throw new Exception("Unable to find method: " + Name);

            return method;
        }

        #region Serialization

        private SerializedNodeClassMethod? SavedDataDuringDeserializationStep1 { get; set; }
        internal static NodeClassMethod Deserialize(NodeClass owner, SerializedNodeClassMethod serializedNodeClassMethod)
        {
            var returnType = TypeBase.Deserialize(owner.Project.TypeFactory, serializedNodeClassMethod.ReturnType);
            var graph = new Graph();
            var nodeClassMethod = new NodeClassMethod(owner, serializedNodeClassMethod.Name, returnType, graph, serializedNodeClassMethod.IsStatic);
            graph.SelfMethod = nodeClassMethod; // a bit / really ugly

            foreach (var parameter in serializedNodeClassMethod.Parameters)
                nodeClassMethod.Parameters.Add(NodeClassMethodParameter.Deserialize(owner.Project.TypeFactory, parameter, nodeClassMethod));

            nodeClassMethod.SavedDataDuringDeserializationStep1 = serializedNodeClassMethod;

            return nodeClassMethod;
        }

        public void Deserialize_Step3()
        {
            if (SavedDataDuringDeserializationStep1 == null)
                throw new Exception("Cannot call Deserialize_Step3 before calling Deserialize");

            Graph.Deserialize(SavedDataDuringDeserializationStep1.Graph, Graph);

            SavedDataDuringDeserializationStep1 = null;
        }

        internal SerializedNodeClassMethod Serialize()
        {
            var serializedNodeClassMethod = new SerializedNodeClassMethod(Name, ReturnType.SerializeWithFullTypeName(), Parameters.Select(x => x.Serialize()).ToList(), Graph.Serialize(), IsStatic);

            return serializedNodeClassMethod;
        }

        #endregion
    }
}
