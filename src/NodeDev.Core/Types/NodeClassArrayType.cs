using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Types
{
    public class NodeClassArrayType : TypeBase
    {
        public readonly NodeClassType InnerNodeClassType;

        public readonly int NbArrayLevels;

        public NodeClassArrayType(NodeClassType innerClassType, int nbArrayLevels)
        {
            if (nbArrayLevels == 0)
                throw new ArgumentException("NodeClassArrayType cannot have 0 array level. That imples 'not array', in which case NodeClassType should be used instead", nameof(nbArrayLevels));

            InnerNodeClassType = innerClassType;
            NbArrayLevels = nbArrayLevels;
        }

        public override string Name => InnerNodeClassType.Name + GetArrayString(NbArrayLevels);

        public override string FullName => InnerNodeClassType.FullName + GetArrayString(NbArrayLevels);

        public override TypeBase[] Generics => InnerNodeClassType.Generics;

        public override TypeBase? BaseType => InnerNodeClassType.NodeClass.TypeFactory.Get<Array>();

        public override TypeBase[] Interfaces => [InnerNodeClassType.NodeClass.TypeFactory.Get(typeof(IReadOnlyList<>), [ArrayInnerType])];

        public override bool IsArray => true;

        public override string FriendlyName => InnerNodeClassType.FriendlyName + GetArrayString(NbArrayLevels);

        public override TypeBase ArrayInnerType => NbArrayLevels == 1 ? InnerNodeClassType : new NodeClassArrayType(InnerNodeClassType, NbArrayLevels - 1);

        public override TypeBase ArrayType => new NodeClassArrayType(InnerNodeClassType, NbArrayLevels + 1);

        public override TypeBase CloneWithGenerics(TypeBase[] newGenerics)
        {
            return new NodeClassArrayType((NodeClassType)InnerNodeClassType.CloneWithGenerics(newGenerics), NbArrayLevels);
        }

        public override IEnumerable<IMemberInfo> GetMembers()
        {
            return [];
        }

        public override IEnumerable<IMethodInfo> GetMethods()
        {
            return [];
        }

        public override IEnumerable<IMethodInfo> GetMethods(string name)
        {
            return [];
        }

        public override bool IsSameBackend(TypeBase typeBase)
        {
            if(typeBase is not NodeClassArrayType nodeClassArrayType)
                return false;

            return NbArrayLevels == nodeClassArrayType.NbArrayLevels && InnerNodeClassType.IsSameBackend(nodeClassArrayType.InnerNodeClassType);
        }

        public override Type MakeRealType()
        {
            var realBaseType = InnerNodeClassType.MakeRealType();

            for(int i = 0; i < NbArrayLevels; i++)
                realBaseType = realBaseType.MakeArrayType();

            return realBaseType;
        }

        private record class SerializedNodeClassArrayType(string InnerNodeClassType, int NbArrayLevels);
        protected internal override string Serialize()
        {
            return System.Text.Json.JsonSerializer.Serialize(new SerializedNodeClassArrayType(InnerNodeClassType.Serialize(), NbArrayLevels));
        }

        public new static NodeClassArrayType Deserialize(TypeFactory typeFactory, string serializedString)
        {
            var deserialized = System.Text.Json.JsonSerializer.Deserialize<SerializedNodeClassArrayType>(serializedString);
            if(deserialized == null)
                throw new ArgumentException("Failed to deserialize NodeClassArrayType");

            return new NodeClassArrayType(NodeClassType.Deserialize(typeFactory, deserialized.InnerNodeClassType), deserialized.NbArrayLevels);
        }


        public static string GetArrayString(int nbArrayLevels)
        {
            if (nbArrayLevels == 0)
                return string.Empty;

            var str = string.Create(nbArrayLevels * 2, nbArrayLevels, static (span, nbLevels) =>
            {
                for (int i = 0; i < span.Length; i += 2)
                {
                    span[i] = '[';
                    span[i + 1] = ']';
                }
            });

            return str;
        }
    }
}
