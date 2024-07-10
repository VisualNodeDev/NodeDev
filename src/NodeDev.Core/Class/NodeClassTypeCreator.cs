using System.Reflection.Emit;
using System.Reflection;
using NodeDev.Core.Types;
using FastExpressionCompiler;
using Dis2Msil;
using System.Text;

namespace NodeDev.Core.Class;

public class NodeClassTypeCreator
{
    public record class GeneratedType(Type Type, TypeBuilder HiddenType, Dictionary<NodeClassMethod, MethodBuilder> Methods);
    public Dictionary<TypeBase, GeneratedType> GeneratedTypes = [];

    public static string HiddenName(string name) => $"_hidden_{name}";
    private static GeneratedType CreateGeneratedType(ModuleBuilder mb, string name) => new(mb.DefineType(name, TypeAttributes.Public | TypeAttributes.Class), mb.DefineType(HiddenName(name), TypeAttributes.NotPublic | TypeAttributes.Class | TypeAttributes.Sealed), []);

    public Assembly? Assembly { get; private set; }

    public readonly Project Project;

    public readonly BuildOptions Options;

    public bool IsPreBuilt => Assembly != null;

    public bool IsBuilt => IsPreBuilt && !Options.PreBuildOnly;

    internal NodeClassTypeCreator(Project project, BuildOptions buildOptions)
    {
        Project = project;
        Options = buildOptions;
    }

    public void CreateProjectClassesAndAssembly()
    {
        // https://learn.microsoft.com/en-us/dotnet/api/system.reflection.emit.assemblybuilder?view=net-7.0
        var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("NodeProject_" + this.Project.Id.ToString().Replace('-', '_')), AssemblyBuilderAccess.RunAndCollect);
        Assembly = ab;

        // The module name is usually the same as the assembly name.
        var mb = ab.DefineDynamicModule(ab.GetName().Name!);

        // Creating all the types early so they are all accessible during expression tree generation
        foreach (var nodeClass in Project.Classes)
        {
            GeneratedType generatedType;
            if (GeneratedTypes.ContainsKey(Project.GetNodeClassType(nodeClass)))
                generatedType = GeneratedTypes[Project.GetNodeClassType(nodeClass)];
            else
                GeneratedTypes[Project.GetNodeClassType(nodeClass)] = generatedType = CreateGeneratedType(mb, nodeClass.Name);

        }

        // Create the properties and methods in the real type
        foreach (var nodeClass in Project.Classes)
        {
            var generatedType = GeneratedTypes[Project.GetNodeClassType(nodeClass)];

            var typeBuilder = (TypeBuilder)generatedType.Type;

            // Define an empty constructor with no parameters
            ConstructorBuilder ctor = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                Type.EmptyTypes);

            ILGenerator ctor0IL = ctor.GetILGenerator();
            ctor0IL.Emit(OpCodes.Ret);

            // Create properties before methods in case a method uses a property
            foreach (var property in nodeClass.Properties)
                GenerateProperty(property, typeBuilder);

            foreach (var method in nodeClass.Methods)
            {
                // create the method in the real type as well as an empty placeholder in the hidden type
                GenerateRealMethodAndEmptyHiddenMethod(method, typeBuilder, generatedType);
            }

            // Create the real type
            // At this point the real type has everything it needs to be created
            // It's methods are simply calling the hidden class methods, but those are still empty
            GeneratedTypes[Project.GetNodeClassType(nodeClass)] = generatedType with
            {
                Type = typeBuilder.CreateType()
            };
        }

        if (!Options.PreBuildOnly)
        {
            // Create the body of each methods in the hidden type
            foreach (var generatedType in GeneratedTypes.Values)
            {
                foreach ((var method, var methodBuilder) in generatedType.Methods)
                    GenerateHiddenMethodBody(method, methodBuilder);

                // We are finally ready to create the final hidden type
                generatedType.HiddenType.CreateType();
            }
        }
    }

    public void GetBodyAsCsAndMsilCode(NodeClassMethod method, out string cs, out string msil)
    {
        if (!IsPreBuilt)
            throw new Exception($"Unable to get method's body as CS code. Assembly must be built before");

        var generated = GeneratedTypes[method.DeclaringType];
        var builder = generated.Methods[method];

        var expression = method.Graph.BuildExpression(Options.BuildExpressionOptions);

        var generator = builder.GetILGenerator();
        expression.CompileFastToIL(generator, CompilerFlags.ThrowOnNotSupportedExpression);

        var str = new StringBuilder();

        str.Append("public ");
        if(method.IsStatic)
            str.Append("static ");
        str.Append(method.ReturnType.FriendlyName);
        str.Append(' ');
        str.Append(method.Name);
        str.Append('(');
        for (int i = 0; i < method.Parameters.Count; ++i)
        {
            if (i > 0)
                str.Append(", ");
            str.Append(method.Parameters[i].ParameterType.FriendlyName);
            str.Append(' ');
            str.Append(method.Parameters[i].Name);
        }
        str.AppendLine(")");
        str.AppendLine("{");
        str.Append(new string(' ', 4));
        expression.Body.ToCSharpString(str, lineIdent: 4);
        str.AppendLine();
        str.Append('}');

        cs = str.ToString();

        dynamic type = generated.HiddenType.CreateType();
        for(int i = 0; i < type.DeclaredMethods.Length; ++i)
        {
            if (type.DeclaredMethods[i].Name == method.Name)
            {
                var methodInfo = (MethodInfo)type.DeclaredMethods[i];
                var bytes = methodInfo.GetMethodBody()!.GetILAsByteArray();
                var reader = new MethodBodyReader(builder.Module, bytes!);
                var code = reader.GetBodyCode();

                msil = code;
                return;
            }
        }

        msil = "method not found";
    }

    private void GenerateHiddenMethodBody(NodeClassMethod method, MethodBuilder methodBuilder)
    {
        // Generate the expression tree for the method
        var expression = method.Graph.BuildExpression(Options.BuildExpressionOptions);

        var ilGenerator = methodBuilder.GetILGenerator();
        var result = expression.CompileFastToIL(ilGenerator, CompilerFlags.ThrowOnNotSupportedExpression);

        if (!result)
            throw new Exception($"Failed to compile the expression tree");
    }

    private static void GenerateRealMethodAndEmptyHiddenMethod(NodeClassMethod method, TypeBuilder typeBuilder, GeneratedType generatedType)
    {
        // create the method
        var parameterTypes = method.Parameters.Select(p => p.ParameterType.MakeRealType()).ToArray();
        var hiddenParameterTypes = parameterTypes;
        var returnType = method.ReturnType.MakeRealType();

        var methodAttributes = MethodAttributes.Public;
        if (method.IsStatic)
            methodAttributes |= MethodAttributes.Static;
        else
            hiddenParameterTypes = hiddenParameterTypes.Prepend(generatedType.Type).ToArray();

        // create the hidden method
        var hiddenMethod = generatedType.HiddenType.DefineMethod(method.Name, MethodAttributes.Static, CallingConventions.Standard, returnType, hiddenParameterTypes);
        generatedType.Methods[method] = hiddenMethod;

        // Create the real method that calls the hidden method
        var realMethod = typeBuilder.DefineMethod(method.Name, methodAttributes, returnType, parameterTypes);
        var ilGenerator = realMethod.GetILGenerator();

        // Load the arguments
        var nbArguments = parameterTypes.Length + (method.IsStatic ? 0 : 1);
        for (var i = 0; i < nbArguments; i++)
        {
            ilGenerator.Emit(OpCodes.Ldarg, i);
        }

        ilGenerator.EmitCall(OpCodes.Call, hiddenMethod, null);
        ilGenerator.Emit(OpCodes.Ret); // if there was any value to return, that value was already put on the stack by the hidden method's call
    }

    private static void GenerateProperty(NodeClassProperty property, TypeBuilder typeBuilder)
    {
        var propertyType = property.PropertyType.MakeRealType();

        // Add a private field that the property will wrap
        var propertyHiddenField = typeBuilder.DefineField($"___{property.Name}", propertyType, FieldAttributes.Private);


        // Define a property named Number that gets and sets the private
        // field.
        //
        // The last argument of DefineProperty is null, because the
        // property has no parameters. (If you don't specify null, you must
        // specify an array of Type objects. For a parameterless property,
        // use the built-in array with no elements: Type.EmptyTypes)
        var propertyBuilder = typeBuilder.DefineProperty(property.Name, PropertyAttributes.HasDefault, propertyType, null);

        // The property "set" and property "get" methods require a special
        // set of attributes.
        var getSetAttr = MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig;

        // Define the "get" accessor method for Number. The method returns
        // an integer and has no arguments. (Note that null could be
        // used instead of Types.EmptyTypes)
        var mbNumberGetAccessor = typeBuilder.DefineMethod(
            $"get_{property.Name}",
            getSetAttr,
            propertyType,
            Type.EmptyTypes);

        var numberGetIL = mbNumberGetAccessor.GetILGenerator();
        // For an instance property, argument zero is the instance. Load the
        // instance, then load the private field and return, leaving the
        // field value on the stack.
        numberGetIL.Emit(OpCodes.Ldarg_0);
        numberGetIL.Emit(OpCodes.Ldfld, propertyHiddenField);
        numberGetIL.Emit(OpCodes.Ret);

        // Define the "set" accessor method for Number, which has no return
        // type and takes one argument of type int (Int32).
        var mbNumberSetAccessor = typeBuilder.DefineMethod(
            $"set_{property.Name}",
            getSetAttr,
            null,
            [propertyType]);

        var numberSetIL = mbNumberSetAccessor.GetILGenerator();
        // Load the instance and then the numeric argument, then store the
        // argument in the field.
        numberSetIL.Emit(OpCodes.Ldarg_0);
        numberSetIL.Emit(OpCodes.Ldarg_1);
        numberSetIL.Emit(OpCodes.Stfld, propertyHiddenField);
        numberSetIL.Emit(OpCodes.Ret);

        // Last, map the "get" and "set" accessor methods to the
        // PropertyBuilder. The property is now complete.
        propertyBuilder.SetGetMethod(mbNumberGetAccessor);
        propertyBuilder.SetSetMethod(mbNumberSetAccessor);
    }

}
