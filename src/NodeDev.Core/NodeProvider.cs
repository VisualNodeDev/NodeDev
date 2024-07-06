using NodeDev.Core.Class;
using NodeDev.Core.Connections;
using NodeDev.Core.Nodes;
using NodeDev.Core.Types;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NodeDev.Core
{
    // this whole way of searching nodes is disgusting.
    // you're welcome
    public static class NodeProvider
    {
        private static List<Type> NodeTypes = new();
        public static void Initialize()
        {
            AddNodesFromAssembly(typeof(NodeProvider).Assembly);
        }


        // function load a list of all class that inherit from Node
        public static void AddNodesFromAssembly(Assembly assembly)
        {
            var types = assembly.GetTypes().Where(p => typeof(Node).IsAssignableFrom(p) && !p.IsAbstract);

            NodeTypes.AddRange(types);
        }

        public record class NodeSearchResult(Type Type);
        public record class MethodCallNode(Type Type, IMethodInfo MethodInfo) : NodeSearchResult(Type);
        public record class GetPropertyOrFieldNode(Type Type, IMemberInfo MemberInfo) : NodeSearchResult(Type);
        public record class SetPropertyOrFieldNode(Type Type, IMemberInfo MemberInfo) : NodeSearchResult(Type);
        public static IEnumerable<NodeSearchResult> Search(Project project, string text, Connection? startConnection)
        {
            var nodes = NodeTypes.Where(x => x != typeof(MethodCall)).Where(p => p.Name.Contains(text, StringComparison.OrdinalIgnoreCase));

            var results = nodes.Select(x => new NodeSearchResult(x));

            IEnumerable<NodeSearchResult> GetPropertiesAndFields(TypeBase type, string text)
            {
                IEnumerable<IMemberInfo> members = type.GetMembers();
                members = members.Where(x => x.Name.Contains(text, StringComparison.OrdinalIgnoreCase)); // filter with the name

                IEnumerable<NodeSearchResult> results = members.Where(x => x.CanGet).Select(x => new GetPropertyOrFieldNode(typeof(GetPropertyOrField), x));
                results = results.Concat(members.Where(x => x.CanGet).Select(x => new SetPropertyOrFieldNode(typeof(SetPropertyOrField), x)));

                return results;
            }

            // check if the text is a method call like 'ClassName.MethodName'
            var methodCallSplit = text.Split('.');
            if (methodCallSplit.Length == 2)
            {
                // try to find the class specified
                project.TypeFactory.CreateBaseFromUserInput(methodCallSplit[0], out var type);
                if (type != null)
                {
                    // find if the method exists
                    var methods = type.GetMethods().Where(x => x.Name.Contains(methodCallSplit[1], StringComparison.OrdinalIgnoreCase));

                    results = results.Concat(methods.Select(x => new MethodCallNode(typeof(MethodCall), x)));

                    results = results.Concat(GetPropertiesAndFields(type, methodCallSplit[1]));
                }
            }
            else if (startConnection?.Type is RealType realType)
            {
                // find if the method exists
                IEnumerable<MethodInfo> methods = realType.BackendType.GetMethods(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance);
                // get extensions methods for the realType.BackendType

                methods = methods.Concat(GetExtensionMethods(realType, project.TypeFactory)).Where(x => string.IsNullOrWhiteSpace(text) || x.Name.Contains(text, StringComparison.OrdinalIgnoreCase));

                results = results.Concat(methods.Select(x => new MethodCallNode(typeof(MethodCall), new RealMethodInfo(project.TypeFactory, x, realType))));

                results = results.Concat(GetPropertiesAndFields(realType, text));
            }
            else if (startConnection?.Type is NodeClassType nodeClassType)
            {
                // get the properties in that object
                results = results.Concat(nodeClassType.NodeClass.Properties.Select(x => new GetPropertyOrFieldNode(typeof(GetPropertyOrField), x)));

                results = results.Concat(nodeClassType.NodeClass.Methods.Select(x => new MethodCallNode(typeof(MethodCall), x)));
            }

            // add methods, get properties and set properties
            results = results.Concat(project.Classes.SelectMany(nodeClass => nodeClass.Methods.Where(x => string.IsNullOrWhiteSpace(text) || x.Name.Contains(text, StringComparison.OrdinalIgnoreCase)).Select(x => new MethodCallNode(typeof(MethodCall), x))));
            results = results.Concat(project.Classes.SelectMany(nodeClass => nodeClass.Properties.Select(x => new GetPropertyOrFieldNode(typeof(GetPropertyOrField), x))));
            results = results.Concat(project.Classes.SelectMany(nodeClass => nodeClass.Properties.Select(x => new SetPropertyOrFieldNode(typeof(SetPropertyOrField), x))));

            // remove any duplicates that may have introduced itself
            results = results.DistinctBy(result =>
            {
                if (result is MethodCallNode methodCallNode)
                    return (object)methodCallNode.MethodInfo;
                //if (result is GetPropertyOrFieldNode propertyOrFieldNode)
                //	return (object)propertyOrFieldNode.MemberInfo;
                //if (result is SetPropertyOrFieldNode getPropertyOrFieldNode)
                //	return (object)getPropertyOrFieldNode.MemberInfo;
                return (object)result;
            });


            return results;
        }

        private static IEnumerable<MethodInfo> GetExtensionMethods(TypeBase t, TypeFactory typeFactory)
        {
            var query = AppDomain.CurrentDomain.GetAssemblies()
                .Where(x => !x.IsDynamic) // dirty patch to prevent loading types from the generated assemblies
                .SelectMany(x => x.GetTypes())
                .Where(type => !type.IsGenericType)
                .SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                .Where(method => method.IsDefined(typeof(ExtensionAttribute), false) && t.IsAssignableTo(typeFactory.Get(method.GetParameters()[0].ParameterType, null), out _));

            return query;
        }
    }
}
