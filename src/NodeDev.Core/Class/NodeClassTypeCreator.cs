﻿using System.Reflection.Emit;
using System.Reflection;
using NodeDev.Core.Types;
using FastExpressionCompiler;
using System.Reflection.PortableExecutable;

namespace NodeDev.Core.Class;

public class NodeClassTypeCreator
{
	public record class GeneratedType(Type Type, TypeBuilder HiddenType, Dictionary<NodeClassMethod, MethodBuilder> Methods);
	public Dictionary<TypeBase, GeneratedType> GeneratedTypes = [];

	public static string HiddenName(string name) => $"_hidden_{name}";
	private static GeneratedType CreateGeneratedType(ModuleBuilder mb, string name) => new(mb.DefineType(name, TypeAttributes.Public | TypeAttributes.Class), mb.DefineType(HiddenName(name), TypeAttributes.NotPublic | TypeAttributes.Class | TypeAttributes.Sealed), []);

	public Assembly CreateProjectClassesAndAssembly(Project project)
	{
		// https://learn.microsoft.com/en-us/dotnet/api/system.reflection.emit.assemblybuilder?view=net-7.0
		var ab = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("NodeProject_" + project.Id.ToString().Replace('-', '_')), AssemblyBuilderAccess.Run);

		// The module name is usually the same as the assembly name.
		var mb = ab.DefineDynamicModule(ab.GetName().Name!);

		// Creating all the types early so they are all accessible during expression tree generation
		foreach (var nodeClass in project.Classes)
		{
			GeneratedType generatedType;
			if (GeneratedTypes.ContainsKey(project.GetNodeClassType(nodeClass)))
				generatedType = GeneratedTypes[project.GetNodeClassType(nodeClass)];
			else
				GeneratedTypes[project.GetNodeClassType(nodeClass)] = generatedType = CreateGeneratedType(mb, nodeClass.Name);

		}

		// Create the properties and methods in the real type
		foreach (var nodeClass in project.Classes)
		{
			var generatedType = GeneratedTypes[project.GetNodeClassType(nodeClass)];

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
			GeneratedTypes[project.GetNodeClassType(nodeClass)] = generatedType with
			{
				Type = typeBuilder.CreateType()
			};
		}

		// Create the body of each methods in the hidden type
		foreach (var generatedType in GeneratedTypes.Values)
		{
			foreach ((var method, var methodBuilder) in generatedType.Methods)
				GenerateHiddenMethodBody(method, methodBuilder);

			// We are finally ready to create the final hidden type
			generatedType.HiddenType.CreateType();
		}

		return ab;
	}

	private static void GenerateHiddenMethodBody(NodeClassMethod method, MethodBuilder methodBuilder)
	{
		// Generate the expression tree for the method
		var expression = method.Graph.BuildExpression(new());

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
		var hiddenMethod = generatedType.HiddenType.DefineMethod(method.Name, MethodAttributes.Assembly | MethodAttributes.Static, CallingConventions.Standard, returnType, hiddenParameterTypes);
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
		ilGenerator.Emit(OpCodes.Ret);
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
