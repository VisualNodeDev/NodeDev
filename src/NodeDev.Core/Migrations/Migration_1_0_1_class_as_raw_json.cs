using System.Text.Json.Nodes;

namespace NodeDev.Core.Migrations;

// really unclean code to convert the old json file formats to the new ones
// The new ones are cleaner and have less sub strings that are already serialized 
internal class Migration_1_0_1_class_as_raw_json : MigrationBase
{
	internal override string Version => "1.0.1";

	internal override void PerformMigrationBeforeDeserialization(JsonObject document)
	{
		var documentClasses = document["Classes"]!;
		foreach (var nodeClass in documentClasses.AsArray().ToList())
		{
			var nodeClassSerializedObject = JsonNode.Parse(nodeClass!.ToString()) ?? throw new Exception("Unable to deserialize node class during migration 1.0.1");

			var classProperties = nodeClassSerializedObject["Properties"]!;
			foreach (var property in classProperties.AsArray().ToList())
			{
				var serializedProperty = JsonNode.Parse(property!.ToString()) ?? throw new Exception("Unable to deserialize node class property during migration 1.0.1");
				classProperties[property!.GetElementIndex()] = serializedProperty;
			}

			var classMethods = nodeClassSerializedObject["Methods"]!;
			foreach (var method in classMethods.AsArray().ToList())
			{
				var serializedMethod = JsonNode.Parse(method!.ToString()) ?? throw new Exception("Unable to deserialize node class method during migration 1.0.1");

				serializedMethod["ReturnType"] = JsonNode.Parse(serializedMethod["ReturnType"]!.ToString()) ?? throw new Exception("Unable to parse return type during migration 1.0.1");

				var methodParameters = serializedMethod["Parameters"]!;
				foreach (var parameter in methodParameters.AsArray().ToList())
				{
					var serializedParameter = JsonNode.Parse(parameter!.ToString()) ?? throw new Exception("Unable to deserialize node class method parameter during migration 1.0.1");

					serializedParameter["ParameterType"] = JsonNode.Parse(serializedParameter["ParameterType"]!.ToString()) ?? throw new Exception("Unable to parse parameter type during migration 1.0.1");
					methodParameters[parameter!.GetElementIndex()] = serializedParameter;
				}

				var graphMethod = JsonNode.Parse(serializedMethod["Graph"]!.ToString()) ?? throw new Exception("Unable to parse graph during migration 1.0.1");

				var graphNodes = graphMethod["Nodes"]!;
				foreach (var node in graphNodes.AsArray().ToList())
				{
					var serializedNode = JsonNode.Parse(node!.ToString()) ?? throw new Exception("Unable to deserialize node during migration 1.0.1");

					var nodeInputs = serializedNode["Inputs"]!;
					foreach (var input in nodeInputs.AsArray().ToList())
					{
						var serializedInput = JsonNode.Parse(input!.ToString()) ?? throw new Exception("Unable to deserialize node input during migration 1.0.1");

						serializedInput["SerializedType"] = JsonNode.Parse(serializedInput["SerializedType"]!.ToString()) ?? throw new Exception("Unable to parse input type during migration 1.0.1");
						nodeInputs[input!.GetElementIndex()] = serializedInput;
					}

					var nodeOutputs = serializedNode["Outputs"]!;
					foreach (var output in nodeOutputs.AsArray().ToList())
					{
						var serializedOutput = JsonNode.Parse(output!.ToString()) ?? throw new Exception("Unable to deserialize node output during migration 1.0.1");

						serializedOutput["SerializedType"] = JsonNode.Parse(serializedOutput["SerializedType"]!.ToString()) ?? throw new Exception("Unable to parse output type during migration 1.0.1");
						nodeOutputs[output!.GetElementIndex()] = serializedOutput;
					}

					graphNodes[node!.GetElementIndex()] = serializedNode;
				}

				serializedMethod["Graph"] = graphMethod;
				classMethods[method!.GetElementIndex()] = serializedMethod;
			}


			documentClasses[nodeClass!.GetElementIndex()] = nodeClassSerializedObject;
		}
	}
}
