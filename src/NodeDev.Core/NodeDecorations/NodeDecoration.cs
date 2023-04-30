using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core.NodeDecorations;

public interface INodeDecoration
{
    public static INodeDecoration Deserialize(string serialized) => throw new NotImplementedException();

    public abstract string Serialize();
}
