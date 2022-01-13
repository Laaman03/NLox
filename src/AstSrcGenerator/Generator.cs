﻿using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;

namespace SourceGenerator
{
    [Generator]
    public class AstGenerator : ISourceGenerator
    {
        // STATEMENTS
        private static readonly ClassDef stmt = new ClassDef
        {
            Name = "Stmt",
            NestedClasses = new NestedClass[]
            {
                new NestedClass { Name = "Block", Fields = "List<Stmt> Statements" },
                new NestedClass { Name = "Expression", Fields = "Expr ExpressionValue" },
                new NestedClass { Name = "If", Fields = "Expr Condition, Stmt ThenBranch, Stmt ElseBranch" },
                new NestedClass { Name = "Print", Fields = "Expr ExpressionValue" },
                new NestedClass { Name = "Var", Fields = "Token Name, Expr Initializer" },
            }
        };

        // EXPRESSIONS
        private static readonly ClassDef expr = new ClassDef
        {
            Name = "Expr",
            NestedClasses = new NestedClass[]
            {
                new NestedClass {Name = "Logical", Fields = "Expr Left, Token Op, Expr Right" },
                new NestedClass { Name = "Binary", Fields = "Expr Left, Token Op, Expr Right"},
                new NestedClass { Name = "Grouping", Fields = "Expr Expression"},
                new NestedClass { Name = "Literal", Fields = "Object Value"},
                new NestedClass { Name = "Unary", Fields = "Token Op, Expr Right"},
                new NestedClass { Name = "Variable", Fields = "Token Name"},
                new NestedClass { Name = "Assign", Fields = "Token Name, Expr Expression"}
            }
        };
        public void Execute(GeneratorExecutionContext ctx)
        {
            var defs = new ClassDef[] { stmt, expr };
            foreach(var def in defs)
            {
// CODE BEGIN *************************************
                var code = $@"// Autogenerated
using System;
using System.Collections.Generic;
namespace NLox.Lib
{{
    public abstract class {def.Name}
    {{
        public abstract R Accept<R>(IVisitor<R> visitor);
        {def.VisitorDef(2)}

        {string.Join("\r\n        ", def.NestedClasses.Select(nc => nc.BuildDefinition(def.Name, 2)))}
    }}
    
}}";
                // CODE END ***************************************
                ctx.AddSource($"{def.Name}.g.cs", code);
            }
        }

        public void Initialize(GeneratorInitializationContext ctx)
        { }
    }
    class ClassDef
    {
        public string Name { get; set; }
        public NestedClass[] NestedClasses { get; set; }
        public string VisitorDef(int indentLvl)
        {
            var lines = new List<string>();

            // Start interface def
            lines.Add("public interface IVisitor<R>");
            lines.Add("{");
            lines.AddRange(NestedClasses.Select(nc => $"    R Visit{nc.Name}{Name}({nc.Name} _{nc.Name.ToLower()});"));
            lines.Add("}");
            var tabs = new string(' ', indentLvl * 4);
            return string.Join($"\r\n{tabs}", lines.ToArray());
        }
    }
    class NestedClass
    {
        public string Name { get; set; }
        public string Fields { get; set; }
        public string BuildDefinition(string baseClass, int indentLvl)
        {
            var lines = new List<string>();
            
            // Begin class def
            lines.AddRange(new string[]
            {
                $"public class {Name} : {baseClass}",
                "{",
            });

            var propList = Fields.Split(',').Select(f => f.Trim()).ToArray();
            // fieldList will look like:
            // { "Token Name", "Expr Initializer" }
            // For the constructor we want to lower those
            // variable names so it looks pretty and
            // we don't have to type 'this' all over the place
            var ctorParms = propList.Select(f =>
            {
                var sig = f.Split(' ');
                var type = sig[0];
                var name = sig[1].ToLower();

                return type + " " + name;
            }).ToArray();
            // Property Defs
            lines.AddRange(propList.Select(f => $"    public {f} {{ get; private init; }}"));

            // Constructor Def
            lines.Add($"    public {Name}({string.Join(", ", ctorParms)})");
            lines.Add("    {");
            foreach (var prop in propList)
            {
                var propName = prop.Split(' ')[1];
                var ctorParam = prop.Split(' ')[1].ToLower();
                lines.Add($"    {propName} = {ctorParam};");
            }
            lines.Add("    }");

            // Accept Method
            lines.Add($"    public override R Accept<R>(IVisitor<R> visitor) => visitor.Visit{Name}{baseClass}(this);");

            // Close class def
            lines.Add("}");
            var tabs = new string(' ', indentLvl * 4);
            return string.Join($"\r\n{tabs}", lines.ToArray());
        }
    }
}