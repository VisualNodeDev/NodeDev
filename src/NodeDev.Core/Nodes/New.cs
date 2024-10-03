using NodeDev.Core.Class;
using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Nodes;

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
            // If we don't know the type yet we are constructing an array, there are no overloads to show
            if (Outputs[1].Type is UndefinedGenericType || Outputs[1].Type.IsArray)
                return [];

            if (Outputs[1].Type is RealType realType)
            {
                var constructors = realType.BackendType.GetConstructors();
                return constructors.Select(x => new AlternateOverload(Outputs[1].Type, x.GetParameters().Select(y => new RealMethodParameterInfo(y, TypeFactory, realType)).OfType<IMethodParameterInfo>().ToList())).ToList();
            }
            else if (Outputs[1].Type is NodeClassType nodeClassType)
                return [new(Outputs[1].Type, [])]; // for now, we don't handle custom constructors

            else
                throw new Exception("Unknown type in New node: " + Outputs[1].Type.Name);
        }
    }

    public override List<Connection> GenericConnectionTypeDefined(Connection connection)
    {
        if (Outputs[1].Type.IsArray)
        {
            Inputs.Add(new("Length", this, TypeFactory.Get<int>()));
        }
        else
        {
            var constructor = AlternatesOverloads.First();

            Inputs.AddRange(constructor.Parameters.Select(x => new Connection(x.Name ?? "??", this, x.ParameterType)));
        }

        return [];
    }

    public override void SelectOverload(AlternateOverload overload, out List<Connection> newConnections, out List<Connection> removedConnections)
    {
        removedConnections = Inputs.Skip(1).ToList();
        Inputs.RemoveRange(1, Inputs.Count - 1);

        newConnections = overload.Parameters.Select(x => new Connection(x.Name, this, x.ParameterType)).ToList();
        Inputs.AddRange(newConnections);
    }

    internal override Expression BuildExpression(Dictionary<Connection, Graph.NodePathChunks>? subChunks, BuildExpressionInfo info)
    {
        var type = Outputs[1].Type.MakeRealType();
        if (type.IsArray)
        {
            var length = info.LocalVariables[Inputs[1]];
            return Expression.Assign(info.LocalVariables[Outputs[1]], Expression.NewArrayBounds(type.GetElementType()!, length));
        }
        else
        {
            var argumentTypes = Inputs.Skip(1).Select(x => x.Type.MakeRealType()).ToArray();
            var constructor = type.GetConstructor(argumentTypes);

            if (constructor == null)
                throw new Exception($"Constructor not found: {Outputs[1].Type.FriendlyName}");

            var arguments = Inputs.Skip(1).Select(x => info.LocalVariables[x]).ToArray();
            return Expression.Assign(info.LocalVariables[Outputs[1]], Expression.New(constructor, arguments));
        }
    }
}
