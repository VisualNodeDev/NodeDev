using NodeDev.Core.Connections;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.Nodes.Creation
{
    public class New : NormalFlowNode
    {
        public New(Graph graph, string? id = null) : base(graph, id)
        {
            Name = "New";

            var t1 = TypeFactory.CreateUndefinedGenericType("T1");
            Outputs.Add(new("Obj", this, t1));
        }

        public override IEnumerable<AlternateOverload> AlternatesOverloads
        {
            get 
            {
                if (Outputs[1].Type is not RealType realType)
                    throw new Exception("Output type is not real");

                var constructors = realType.BackendType.GetConstructors();
                return constructors.Select(x => new AlternateOverload(Outputs[1].Type, x.GetParameters().Select(y => (y.Name ?? "??", (TypeBase)TypeFactory.Get(y.ParameterType))).ToList()));
            }
        }
        public override List<Connection> GenericConnectionTypeDefined(UndefinedGenericType previousType)
        {
            var constructor = AlternatesOverloads.First();

            Inputs.AddRange(constructor.Parameters.Select(x => new Connection(x.Name ?? "??", this, x.Type)));

            Name = $"New {Outputs[1].Type.FriendlyName}";
            return new();
        }

        protected override void ExecuteInternal(object?[] inputs, object?[] outputs)
        {
            if (Outputs[1].Type is UndefinedGenericType)
                throw new InvalidOperationException("Output type is not defined");

            if (Outputs[1].Type is RealType realType)
                outputs[1] = Activator.CreateInstance(realType.BackendType);
            else
                throw new InvalidOperationException("Output type is not real");
        }
    }
}
