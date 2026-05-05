namespace Zenvoyce.Application.Abstractions.Services;

/// <summary>
/// Render Handlebars-style template (`{{var}}`, `{{#each items}}`,…) với context object.
/// </summary>
public interface ITemplateRenderer
{
    string Render(string template, object context);
}
