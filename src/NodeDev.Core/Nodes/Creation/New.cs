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
            var t1 = TypeFactory.CreateUndefinedGenericType("T1");
            Outputs.Add(new("Obj", this, t1));
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
