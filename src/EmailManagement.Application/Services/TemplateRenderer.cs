using System.Text.RegularExpressions;
using EmailManagement.Application.Ports;

namespace EmailManagement.Application.Services;

public partial class TemplateRenderer : ITemplateRenderer
{
    [GeneratedRegex(@"\{\{\s*([a-zA-Z_][a-zA-Z0-9_]*)\s*\}\}", RegexOptions.Compiled)]
    private static partial Regex VariablePattern();

    public string Render(string template, IReadOnlyDictionary<string, string> variables, bool htmlEncode = false)
    {
        if (string.IsNullOrEmpty(template))
            return template;

        return VariablePattern().Replace(template, match =>
        {
            var key = match.Groups[1].Value;
            if (variables.TryGetValue(key, out var value))
            {
                return htmlEncode ? System.Net.WebUtility.HtmlEncode(value) : value;
            }
            return match.Value;
        });
    }
}