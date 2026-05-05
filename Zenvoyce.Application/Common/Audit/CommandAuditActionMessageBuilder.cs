using System.Reflection;
using System.Text.RegularExpressions;

namespace Zenvoyce.Application.Common.Audit;

public static class CommandAuditActionMessageBuilder
{
    private const int MaxActionLength = 255;

    private static readonly Regex PascalCaseWordRegex = new(
        "([a-z0-9])([A-Z])",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    private static readonly string[] DescriptorPropertyNames =
    [
        "Name",
        "Ten",
        "Username",
        "Tendangnhap",
        "Code",
        "Ma",
        "Kyhieu",
        "Title",
        "Id"
    ];

    public static string Build(object? command, Guid? userId)
    {
        if (command is null)
            return LimitLength(FormatAction(userId, "thuc hien thao tac", "ban ghi"));

        var type = command.GetType();
        var commandName = type.Name;
        var operationVerb = ResolveOperationVerb(commandName);
        var resource = ResolveResourceName(commandName);
        var descriptor = ResolveDescriptor(command);

        var target = descriptor is null ? resource : $"{resource} ({descriptor})";
        return LimitLength(FormatAction(userId, operationVerb, target));
    }

    private static string ResolveOperationVerb(string commandName)
    {
        if (commandName.StartsWith("Create", StringComparison.Ordinal))
            return "da tao";
        if (commandName.StartsWith("Update", StringComparison.Ordinal))
            return "da cap nhat";
        if (commandName.StartsWith("Delete", StringComparison.Ordinal))
            return "da xoa";

        return "da thuc hien";
    }

    private static string ResolveResourceName(string commandName)
    {
        var baseName = commandName.EndsWith("Command", StringComparison.Ordinal)
            ? commandName[..^"Command".Length]
            : commandName;

        if (baseName.StartsWith("Create", StringComparison.Ordinal))
            baseName = baseName["Create".Length..];
        else if (baseName.StartsWith("Update", StringComparison.Ordinal))
            baseName = baseName["Update".Length..];
        else if (baseName.StartsWith("Delete", StringComparison.Ordinal))
            baseName = baseName["Delete".Length..];

        if (string.IsNullOrWhiteSpace(baseName))
            return "ban ghi";

        var normalized = PascalCaseWordRegex.Replace(baseName, "$1 $2").Trim();
        return normalized.Length == 0 ? "ban ghi" : normalized.ToLowerInvariant();
    }

    private static string? ResolveDescriptor(object command)
    {
        var type = command.GetType();

        foreach (var propertyName in DescriptorPropertyNames)
        {
            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
            if (prop is null || !prop.CanRead || prop.GetIndexParameters().Length > 0)
                continue;

            object? value;
            try
            {
                value = prop.GetValue(command);
            }
            catch
            {
                continue;
            }

            if (value is null)
                continue;

            var text = value switch
            {
                string s => s.Trim(),
                Guid g => g.ToString("D"),
                _ => value.ToString()?.Trim() ?? string.Empty
            };

            if (!string.IsNullOrWhiteSpace(text))
                return text.Length > 60 ? text[..60] : text;
        }

        return null;
    }

    private static string FormatAction(Guid? userId, string verb, string target)
    {
        var actor = userId.HasValue ? $"[{userId.Value:D}]" : "[He thong]";
        return $"{actor} {verb} {target}";
    }

    private static string LimitLength(string action)
        => action.Length <= MaxActionLength ? action : action[..MaxActionLength];
}
