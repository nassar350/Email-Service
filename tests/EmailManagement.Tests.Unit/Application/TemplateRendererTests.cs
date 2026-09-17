using EmailManagement.Application.Services;
using FluentAssertions;

namespace EmailManagement.Tests.Unit.Application;

public class TemplateRendererTests
{
    private readonly TemplateRenderer _renderer = new();

    [Fact]
    public void Render_ReplacesKnownVariables()
    {
        var result = _renderer.Render("Hello {{name}}, order {{orderNo}}",
            new Dictionary<string, string> { ["name"] = "Alice", ["orderNo"] = "ORD-1" });

        result.Should().Be("Hello Alice, order ORD-1");
    }

    [Fact]
    public void Render_PreservesUnknownVariables()
    {
        var result = _renderer.Render("Hello {{name}}", new Dictionary<string, string>());

        result.Should().Be("Hello {{name}}");
    }

    [Fact]
    public void Render_HandlesWhitespaceInsideBraces()
    {
        var result = _renderer.Render("Hi {{ name }}!", new Dictionary<string, string> { ["name"] = "Bob" });

        result.Should().Be("Hi Bob!");
    }

    [Fact]
    public void Render_HtmlEncodesValuesWhenRequested()
    {
        var result = _renderer.Render("<p>{{value}}</p>",
            new Dictionary<string, string> { ["value"] = "<script>alert('x')</script>" }, htmlEncode: true);

        result.Should().Be("<p>&lt;script&gt;alert(&#39;x&#39;)&lt;/script&gt;</p>");
    }

    [Fact]
    public void Render_DoesNotEncodeWhenNotRequested()
    {
        var result = _renderer.Render("{{value}}",
            new Dictionary<string, string> { ["value"] = "<b>bold</b>" });

        result.Should().Be("<b>bold</b>");
    }

    [Fact]
    public void Render_NullOrEmptyTemplate_ReturnsAsIs()
    {
        _renderer.Render(null!, new Dictionary<string, string>()).Should().BeNull();
        _renderer.Render(string.Empty, new Dictionary<string, string>()).Should().BeEmpty();
    }

    [Fact]
    public void Render_DoesNotMatchInvalidVariableNames()
    {
        var result = _renderer.Render("{{1bad}} {{}}", new Dictionary<string, string>());

        result.Should().Be("{{1bad}} {{}}");
    }

    [Fact]
    public void Render_ReplacesRepeatedOccurrences()
    {
        var result = _renderer.Render("{{x}}-{{x}}", new Dictionary<string, string> { ["x"] = "1" });

        result.Should().Be("1-1");
    }
}