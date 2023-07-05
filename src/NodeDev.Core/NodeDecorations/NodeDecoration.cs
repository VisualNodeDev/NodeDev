using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.NodeDecorations;

public interface INodeDecoration
{
    public static INodeDecoration Deserialize(TypeFactory typeFactory, string serialized) => throw new NotImplementedException();

    public abstract string Serialize();
}
