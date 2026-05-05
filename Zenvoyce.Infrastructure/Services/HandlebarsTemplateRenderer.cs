using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using HandlebarsDotNet;
using Zenvoyce.Application.Abstractions.Services;

namespace Zenvoyce.Infrastructure.Services;

/// <summary>
/// Render Handlebars template, cache compiled templates theo hash của template
/// để tránh re-compile cho cùng một mẫu HTML.
/// </summary>
public class HandlebarsTemplateRenderer : ITemplateRenderer
{
    private readonly ConcurrentDictionary<string, HandlebarsTemplate<object, object>> _cache = new();

    public string Render(string template, object context)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return string.Empty;
        }

        var key = ComputeKey(template);
        var compiled = _cache.GetOrAdd(key, _ => Handlebars.Compile(template));
        return compiled(context);
    }

    private static string ComputeKey(string template)
    {
        var bytes = Encoding.UTF8.GetBytes(template);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
