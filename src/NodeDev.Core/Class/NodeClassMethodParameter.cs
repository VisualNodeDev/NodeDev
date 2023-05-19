using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Class
{
	public class NodeClassMethodParameter
	{
		private record class SerializedNodeClassMethodParameter(string Name, string ParameterTypeFullName, string ParameterType);

		public string Name { get; }

		public TypeBase ParameterType { get; }

		public NodeClassMethodParameter(string name, TypeBase parameterType)
		{
			Name = name;
			ParameterType = parameterType;
		}

		public string Serialize()
		{
			return System.Text.Json.JsonSerializer.Serialize(new SerializedNodeClassMethodParameter(Name, ParameterType.GetType().FullName!, ParameterType.Serialize()));
		}

		public static NodeClassMethodParameter Deserialize(string serialized)
		{
			var serializedNodeClassMethodParameter = System.Text.Json.JsonSerializer.Deserialize<SerializedNodeClassMethodParameter>(serialized) ?? throw new Exception("Unable to deserialize node class method parameter");
			return new NodeClassMethodParameter(serializedNodeClassMethodParameter.Name, TypeBase.Deserialize(serializedNodeClassMethodParameter.ParameterTypeFullName, serializedNodeClassMethodParameter.ParameterType));
		}
	}
}
