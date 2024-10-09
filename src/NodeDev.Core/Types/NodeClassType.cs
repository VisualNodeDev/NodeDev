using NodeDev.Core.Class;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Types
{
    public class NodeClassType : TypeBase
    {
        public readonly NodeClass NodeClass;

        public NodeClassType(NodeClass nodeClass, TypeBase[] generics)
        {
            if (generics.Length != 0)
                throw new NotImplementedException("Generics are not supported yet for NodeClass");

            NodeClass = nodeClass;
            Generics = generics;
        }

        public override string Name => NodeClass.Name;

        public override string FullName => NodeClass.Namespace + "." + NodeClass.Name;

        public override TypeBase[] Generics { get; }

        override public TypeBase? BaseType => null;

        public override string FriendlyName => Name;

        public override bool IsArray => false;

        public override TypeBase ArrayInnerType => throw new NotImplementedException();

        public override TypeBase ArrayType => new NodeClassArrayType(this, 1);

        public override TypeBase[] Interfaces => [];

        public override IEnumerable<IMemberInfo> GetMembers() => NodeClass.Properties;

        public NodeClassType GetNonArray()
        {
            if(!IsArray)
                return this;

            return NodeClass.Project.GetNodeClassType(NodeClass, Generics);
        }

        internal protected override string Serialize()
        {
            return FullName;
        }

        public override TypeBase CloneWithGenerics(TypeBase[] newGenerics)
        {
            if (Generics.Length != newGenerics.Length)
                throw new ArgumentException("Generics count mismatch");

            return new NodeClassType(NodeClass, newGenerics);
        }

        public new static NodeClassType Deserialize(TypeFactory typeFactory, string typeName)
        {
            return typeFactory.Project.GetNodeClassType(typeFactory.Project.Classes.First(x => x.Namespace + "." + x.Name == typeName));
        }

        public override IEnumerable<IMethodInfo> GetMethods()
        {
            return NodeClass.Methods;
        }

        public override IEnumerable<IMethodInfo> GetMethods(string name)
        {
            return NodeClass.Methods.Where(x => x.Name == name);
        }

        public override Type MakeRealType()
        {
            return NodeClass.Project.GetCreatedClassType(NodeClass);
        }

        public override bool IsSameBackend(TypeBase typeBase)
        {
            return typeBase is NodeClassType nodeClassType && nodeClassType.NodeClass == NodeClass;
        }
    }
}
