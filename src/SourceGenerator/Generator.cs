using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using System.Text;
using Hydro;

[Generator]
public class HydroTagHelperGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
    }

    IEnumerable<INamedTypeSymbol> GetAllTypes(Compilation compilation)
    {
        List<INamedTypeSymbol> allTypes = new List<INamedTypeSymbol>();
        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var root = syntaxTree.GetRoot();
            var nodes = root.DescendantNodesAndSelf();
            foreach (var node in nodes)
            {
                var symbol = semanticModel.GetDeclaredSymbol(node);
                if (symbol is INamedTypeSymbol namedTypeSymbol)
                {
                    allTypes.Add(namedTypeSymbol);
                }
            }
        }
        return allTypes;
    }
    
    public void Execute(GeneratorExecutionContext context)
    {
        var allTypes = GetAllTypes(context.Compilation);

        foreach (var typeSymbol in allTypes)
        {
            if (!IsHydroComponent(typeSymbol))
            {
                continue;
            }
     
            var sourceCode = new StringBuilder();

            var publicProperties = typeSymbol
                .GetMembers()
                .OfType<IPropertySymbol>()
                .Where(p => 
                    p.DeclaredAccessibility == Accessibility.Public
                    && !p.IsStatic
                    && p.GetAttributes().Any(a => a.AttributeClass?.Name == nameof(ParamAttribute))
                    );

            var tagHelperCode = GenerateTagHelperCode(typeSymbol.Name, publicProperties);
            sourceCode.AppendLine(tagHelperCode);
            
            context.AddSource($"Generated_{typeSymbol.Name}.cs", SourceText.From(sourceCode.ToString(), Encoding.UTF8));
        } }

    private bool IsHydroComponent(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.BaseType?.Name == "HydroComponent";
    }

    private string GenerateTagHelperCode(string componentName, IEnumerable<IPropertySymbol> properties)
    {
        string kebabCaseName = ToKebabCase(componentName);
        string tagHelperName = $"{componentName}TagHelper";
        string targetElement = $"hydro:{componentName}";

        var props = string.Join(Environment.NewLine, properties.Select(p => 
            $"[HtmlAttributeName(\"{ToKebabCase(p.Name)}\")]\npublic {p.Type.ToDisplayString()} {p.Name} {{ get; set; }}"));
        
        var anonymousObjProps = string.Join(", ", properties.Select(p => $"{p.Name} = {p.Name}"));

        return $@"
using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;
using Microsoft.Extensions.DependencyInjection;

namespace Hydro.TagHelpers
{{
    [HtmlTargetElement(""{targetElement}"")]
    public sealed class {tagHelperName} : TagHelper
    {{
        [HtmlAttributeNotBound]
        [ViewContext]
        public ViewContext ViewContext {{ get; set; }}

        {props}

        [HtmlAttributeName(""key"")]
        public string Key {{ get; set; }}

        public override async Task ProcessAsync(TagHelperContext context, TagHelperOutput output)
        {{
            if (ViewContext?.ViewData.Model == null)
            {{
                return;
            }}

            output.TagName = null;
            var viewComponentHelper = ViewContext.HttpContext.RequestServices.GetService<IViewComponentHelper>();
            ((IViewContextAware)viewComponentHelper).Contextualize(ViewContext);

            var componentHtml = await viewComponentHelper.InvokeAsync(""{componentName}"", new
            {{
                {(anonymousObjProps.Length != 0 ? anonymousObjProps + "," : "")}
                key = Key
            }});

            output.Content.SetHtmlContent(componentHtml);
        }}
    }}
}}
";
    }

    private string ToKebabCase(string str)
    {
        StringBuilder sb = new StringBuilder();
        foreach (var c in str)
        {
            if (char.IsUpper(c))
            {
                if (sb.Length > 0)
                {
                    sb.Append('-');
                }
                sb.Append(char.ToLower(c));
            }
            else
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
