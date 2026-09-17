using EmailManagement.Domain.Enums;

namespace EmailManagement.Application.Ports;

public interface ITemplateRenderer
{
    string Render(string template, IReadOnlyDictionary<string, string> variables, bool htmlEncode = false);
}