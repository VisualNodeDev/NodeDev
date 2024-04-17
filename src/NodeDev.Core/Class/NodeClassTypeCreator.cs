using System.Reflection.Emit;
using System.Reflection;
using NodeDev.Core.Types;
using System.Linq.Expressions;

namespace NodeDev.Core.Class;

public class NodeClassTypeCreator
{
    public Dictionary<TypeBase, Type> GeneratedTypes = new();

    public static Delegate Test(int methodIndex)
    {
        var c = Expression.Parameter(typeof(float), "a");

        var lambdaExpr = Expression.Lambda(Expression.Multiply(c, Expression.Constant(2f)), c);
        var lambda = lambdaExpr.Compile();

        return lambda;
    }

    private Type TryGetTypeDuringAssemblyCreation(TypeBase typeBase, ModuleBuilder mb)
    {
        Type propertyType;
        if (typeBase is RealType realType)
            propertyType = realType.MakeRealType();
        else if (typeBase is NodeClassType nodeClassType) // check if the class was already built or create the empty class right now
        {
            if (GeneratedTypes.ContainsKey(nodeClassType))
                propertyType = GeneratedTypes[nodeClassType];
            else
                GeneratedTypes[nodeClassType] = propertyType = mb.DefineType(typeBase.Name, TypeAttributes.Public);
        }
        else
            throw new Exception("Unknown property type, unable to generate IL");

        return propertyType;
    }

    public Assembly CreateProjectClassesAndAssembly(Project project)
    {
        // https://learn.microsoft.com/en-us/dotnet/api/system.reflection.emit.assemblybuilder?view=net-7.0
        var dynamicAssembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("NodeProject_" + project.Id.ToString().Replace('-', '_')), AssemblyBuilderAccess.Run);

        // The module name is usually the same as the assembly name.
        var moduleBuilder = dynamicAssembly.DefineDynamicModule(dynamicAssembly.GetName().Name!);

        foreach (var nodeClass in project.Classes)
        {
            TypeBuilder typeBuilder;
            if (GeneratedTypes.ContainsKey(project.GetNodeClassType(nodeClass)))
                typeBuilder = (TypeBuilder)GeneratedTypes[project.GetNodeClassType(nodeClass)];
            else
                GeneratedTypes[project.GetNodeClassType(nodeClass)] = typeBuilder = moduleBuilder.DefineType(nodeClass.Name, TypeAttributes.Public);

            // Define a default constructor that supplies a default value
            // for the private field. For parameter types, pass the empty
            // array of types or pass null.
            ConstructorBuilder ctor0 = typeBuilder.DefineConstructor(
                MethodAttributes.Public,
                CallingConventions.Standard,
                Type.EmptyTypes);

            ILGenerator ctor0IL = ctor0.GetILGenerator();
            ctor0IL.Emit(OpCodes.Ret);

            foreach (var property in nodeClass.Properties)
            {
                var propertyType = TryGetTypeDuringAssemblyCreation(property.PropertyType, moduleBuilder);

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
                    new Type[] { propertyType });

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

            foreach (var method in nodeClass.Methods)
            {
                var typeArgs = method.Parameters.Select(p => TryGetTypeDuringAssemblyCreation(p.ParameterType, moduleBuilder));
                var methodBuilder = typeBuilder.DefineMethod(method.Name, MethodAttributes.Public, TryGetTypeDuringAssemblyCreation(method.ReturnType, moduleBuilder), typeArgs.ToArray());

                var il = methodBuilder.GetILGenerator();

                var getMethodGraphDelegate = typeof(ProjectExecution).GetMethod(nameof(ProjectExecution.GetMethodGraphDelegate))!;

                // pre register the method in the ProjectExecution class. This gives us an id that can be used at runtime to get the delegate
                method.ProjectExecution_MethodDelegateId = ProjectExecution.PreRegisterProjectClassMethod();

                // get the delegate from the ProjectExecution class using the id
                il.Emit(OpCodes.Ldc_I4, method.ProjectExecution_MethodDelegateId);
                il.Emit(OpCodes.Call, getMethodGraphDelegate);

                // create the delegate type. This is used to 'type' the call.
                // It is necessary to be able use 'ref' and 'out' parameters since they don't work with Func<> or Action<>
                if (method.ReturnType != project.TypeFactory.Void)
                    typeArgs = typeArgs.Append(TryGetTypeDuringAssemblyCreation(method.ReturnType, moduleBuilder));
                var delegateType = Expression.GetFuncType(typeArgs.ToArray());


                //il.Emit(OpCodes.Ldarg_0); // load the instance
                for(int i = 0; i < method.Parameters.Count; i++)
                	il.Emit(OpCodes.Ldarg, i + 1); // load the parameters

                // the delegate is on the stack already, with the newly loaded parameters too
                il.Emit(OpCodes.Callvirt, delegateType.GetMethod("Invoke")!);

                // The value returned by the delegate is now on the stack, so we can return it right away
                il.Emit(OpCodes.Ret);
            }

            var type = typeBuilder.CreateType();

            GeneratedTypes[project.GetNodeClassType(nodeClass)] = type;
        }
        // Finish the type.

        return dynamicAssembly;
    }

}
