﻿using System;
using System.Text;
using System.Threading;

using Maid.CSharp.Generators.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

using static Maid.CSharp.Generators.Utils.CodeBuilder;

namespace Maid.CSharp.Generators.StrongId;


[Generator]
public class StrongIdGenerator : IIncrementalGenerator
{
	private const string Namespace = "Maid.CSharp.Generators.StrongIdGenerator";
	private const string StrongIdAttributeName = "StrongIdAttribute";
	private const string FullyQualifiedAttributeName = $"{Namespace}.{StrongIdAttributeName}`1";
	internal static CodeBuilder codeBuilder;
	private static int _counter = 0;


	private static string GetStrongIdAttribute()
	{
		var sb = new StringBuilder();
		sb.AppendLine("// <auto-generated />");
		sb.AppendLine("using System;");
		sb.AppendLine();
		sb.AppendLine($"namespace {Namespace};");
		sb.AppendLine();
		sb.AppendLine("[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]");
		sb.AppendLine($"internal sealed class {StrongIdAttributeName}<TIdType>(): Attribute;");
		return sb.ToString();
	}

	void IIncrementalGenerator.Initialize(IncrementalGeneratorInitializationContext context)
	{
		codeBuilder = new CodeBuilder();
		//var assemblies = AppDomain.CurrentDomain.GetAssemblies();
		//Debug.WriteLine(assemblies.Where(ass => ass.FullName.StartsWith("Maid.CSharp")).Select(ass => ass.FullName));
		//Console.WriteLine(assemblies.Where(ass => ass.FullName.StartsWith("Maid.CSharp")).Select(ass => ass.FullName));
		context.RegisterPostInitializationOutput(
			static context =>
			{
				context.AddSource($"{StrongIdAttributeName}.generated.cs", GetStrongIdAttribute());
			});


		var pipeline = context.SyntaxProvider
			.ForAttributeWithMetadataName(
				fullyQualifiedMetadataName: FullyQualifiedAttributeName,
				predicate: static (node, ct) => node is RecordDeclarationSyntax,
				transform: static (ctx, ct) =>
				{
					var classSymbol = ctx.TargetSymbol;
					return new RecordInfo
					{
						Name = classSymbol.Name,
						Namespace = classSymbol.ContainingNamespace.ToDisplayString(),
						AccessModifier = classSymbol.DeclaredAccessibility.ToString().ToLower(),
						ValueType = ctx.Attributes[0].AttributeClass?.TypeArguments[0].Name ?? string.Empty
					};
				});
		context.RegisterSourceOutput(pipeline, static (ctx, recordInfo) =>
		{
			var code = codeBuilder
		.WithNamespace(recordInfo.Namespace)
		.WithMembers(
			RecordOrClass.CreateRecord(recordInfo.Name, recordInfo.AccessModifier)
			.WithArguments((recordInfo.ValueType, "Value"))
					.MarkPartial()
					.WithBody(
						body =>
						{
							body.WithStatements([
							$"public static implicit operator {recordInfo.Name}({recordInfo.ValueType} id) => new(id);",
									$"public static implicit operator {recordInfo.ValueType}({recordInfo.Name} id) => id.Value;",
										]);
						})
				);
			var sb = new StringBuilder();
			sb.AppendLine("// <auto-generated />");
			//sb.AppendLine($"//Counter: {_counter++}");
			sb.AppendLine($"//Counter: {Interlocked.Increment(ref _counter)}");
			sb.AppendLine(code.GetCode());
			ctx.AddSource($"{recordInfo.Name}.generated.cs",
			   sb.ToString());
		});
	}

}

internal record RecordInfo
{
public string Namespace { get; set; } = string.Empty;
public string Name { get; set; } = string.Empty;
public string AccessModifier { get; set; } = string.Empty;
public string ValueType { get; set; } = string.Empty;
}
