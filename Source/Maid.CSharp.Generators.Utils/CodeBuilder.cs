using System;
using System.Linq;
using System.Text;
namespace Maid.CSharp.Generators.Utils;

public class CodeBuilder
{
	private string _namespace;
	private EquatableList<Member> _members = new();
	private EquatableList<string> _usings = new();

	public string GetCode()
	{
		var sb = new StringBuilder();
		if (_usings.Count >= 1)
		{
			foreach (var member in _usings)
			{
				sb.AppendLine(member);
			}
			sb.AppendLine();
		}
		sb.AppendLine($"namespace {_namespace};");
		foreach (var member in _members)
		{
			sb.AppendLine(member.GetCode());
		}
		return sb.ToString();
	}
	public static CodeBuilder CreateFile() { return new CodeBuilder(); }

	public CodeBuilder WithNamespace(string namespacename)
	{
		_namespace = namespacename;
		return this;
	}

	public CodeBuilder WithMembers(params Member[] members)
	{
		_members.AddRange(members);
		return this;
	}

	public CodeBuilder WithDefaultUsings(params string[] additionalUsings)
	{
		_usings.AddRange(
			[
			"System",
			..additionalUsings
			]);
		return this;
	}


	public abstract record Member
	{
		public abstract string GetCode();
	}

	public record CodeBlock
	{
		private EquatableList<string> _statements = new();

		public CodeBlock WithStatements(params string[] statements)
		{
			_statements.AddRange(statements);
			return this;
		}

		public string GetBodyCode()
		{
			var sb = new StringBuilder();
			foreach (var statement in _statements)
			{
				sb.AppendLine("\t" + statement);
			}
			return sb.ToString();
		}
		public string GetFullBodyCode()
		{
			var sb = new StringBuilder();
			sb.AppendLine("{");
			foreach (var statement in _statements)
			{
				sb.AppendLine("\t" + statement);
			}
			sb.AppendLine("}");
			return sb.ToString();
		}
	}

	public record RecordOrClass : Member
	{
		private string _type;
		private string _name;
		private string _accessModifier;
		private bool _isPartial;
		private bool _isStatic;
		private string _body;
		private string _arguments;
		private string _inheritance;
		private string[] _attributes;

		private bool hasArguments => !string.IsNullOrWhiteSpace(_arguments);
		private bool hasInheritance => !string.IsNullOrWhiteSpace(_inheritance);

		public static implicit operator string(RecordOrClass recordMember) => recordMember.GetCode();

		public static RecordOrClass CreateClass(string name, string accessModifier)
		{
			return new() { _name = name, _accessModifier = accessModifier, _type = "class" };
		}
		public static RecordOrClass CreateRecord(string name, string accessModifier)
		{
			return new() { _name = name, _accessModifier = accessModifier, _type = "record" };
		}

		public override string GetCode()
		{
			var sb = new StringBuilder();

			sb.Append(_accessModifier);
			if (_isStatic)
				sb.Append(" static");
			if (_isPartial)
				sb.Append(" partial");
			sb.Append($" {_type} {_name}{_arguments}");
			if (hasInheritance)
				sb.Append($" : {_inheritance}");
			if (_body is not null)
			{
				sb.AppendLine();
				sb.AppendLine(_body);
			}
			else
			{
				sb.Append(";");
			}
			return sb.ToString();
		}

		public RecordOrClass MarkPartial()
		{
			_isPartial = true;
			return this;
		}
		public RecordOrClass MarkStatic()
		{
			_isStatic = true;
			return this;
		}

		public RecordOrClass WithBody(Action<CodeBlock> transform)
		{
			var body = new CodeBlock();
			transform(body);
			_body = body.GetFullBodyCode();
			return this;
		}
		public RecordOrClass WithArguments(params (string, string)[] arguments)
		{
			var sb = new StringBuilder();
			sb.Append("(");
			sb.Append(string.Join(", ", arguments.Select(a => $"{a.Item1} {a.Item2}")));
			sb.Append(")");
			_arguments = sb.ToString();

			return this;
		}
		public RecordOrClass WithInheritance(string inheritance)
		{
			_inheritance = inheritance;
			return this;
		}
		public RecordOrClass WithAttrbutes(params string[] attributes)
		{
			_attributes = attributes;
			return this;
		}
	}
	public record Method : Member
	{
		private string _methodName;
		private string _accessModifier;
		private string _returnType;
		private string _arguments;
		private string _body;
		private string _expression;
		private bool _isStatic;
		private bool _isPartial;
		private bool _isVirtual;

		private bool hasExpression => !string.IsNullOrWhiteSpace(_expression);
		private bool hasBody => !string.IsNullOrWhiteSpace(_body);
		public override string GetCode()
		{
			var sb = new StringBuilder();
			sb.Append(_accessModifier);
			if (_isStatic)
				sb.Append(" static");
			if (_isVirtual)
				sb.Append(" virtual");
			if (_isPartial)
				sb.Append(" partial");
			sb.Append($" {_returnType} {_methodName}({_arguments})");
			if (hasExpression)
			{
				sb.Append($" => {_expression};");
				return sb.ToString();
			}

			sb.AppendLine();
			sb.AppendLine(_body);
			return sb.ToString();
		}

		public static Method Create(string name, string accessModifier, string returnType)
		{
			return new() { _methodName = name, _accessModifier = accessModifier, _returnType = returnType };
		}
		public Method MarkStatic()
		{
			_isStatic = true;
			return this;
		}
		public Method MarkPartial()
		{
			_isPartial = true;
			return this;
		}
		public Method MarkVirtual()
		{
			_isVirtual = true;
			return this;
		}
		public Method WithBody(Action<CodeBlock> transform)
		{
			if (hasExpression)
				throw new InvalidOperationException("Cannot have both a body and an expression");
			var body = new CodeBlock();
			transform(body);
			_body = body.GetFullBodyCode();
			return this;
		}
		public Method WithExpression(string expression)
		{
			if (hasBody)
				throw new InvalidOperationException("Cannot have both a body and an expression");
			_expression = expression;
			return this;
		}
		public Method WithArguments(params (string, string)[] arguments)
		{
			_arguments = string.Join(", ", arguments.Select(a => $"{a.Item1} {a.Item2}"));
			return this;
		}
	}

}