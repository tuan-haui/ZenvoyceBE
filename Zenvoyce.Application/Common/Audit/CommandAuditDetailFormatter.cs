using System.Globalization;
using System.Reflection;

namespace Zenvoyce.Application.Common.Audit;

/// <summary>
/// Tóm tắt command cho nhật ký (tối đa 200 ký tự, không ghi trường nhạy cảm).
/// </summary>
public static class CommandAuditDetailFormatter
{
    private const int MaxTotalLength = 200;
    private const int MaxValueLength = 48;

    private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "Password",
        "Matkhau",
        "OldPassword",
        "NewPassword",
        "ConfirmPassword",
        "Token",
        "Secret",
        "RefreshToken"
    };

    public static string? Format(object? command)
    {
        if (command is null)
            return null;

        var type = command.GetType();
        var segments = new List<string>();

        foreach (var prop in type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
        {
            if (!prop.CanRead || prop.GetIndexParameters().Length > 0)
                continue;

            if (SensitivePropertyNames.Contains(prop.Name))
                continue;

            if (IsLargeOrBinaryPayloadProperty(prop))
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

            if (value is string s)
            {
                if (s.Length == 0)
                    continue;
            }
            else if (value is byte[] or ReadOnlyMemory<byte> or Memory<byte> or char[])
            {
                continue;
            }

            var formatted = FormatValue(value);
            if (string.IsNullOrEmpty(formatted))
                continue;

            if (formatted.Length > MaxValueLength)
                formatted = formatted[..MaxValueLength] + "…";

            segments.Add($"{prop.Name}={formatted}");
        }

        segments.Sort(StringComparer.Ordinal);
        var combined = string.Join("; ", segments);
        if (combined.Length == 0)
            return null;

        return combined.Length <= MaxTotalLength ? combined : combined[..MaxTotalLength];
    }

    private static bool IsLargeOrBinaryPayloadProperty(PropertyInfo prop)
    {
        var n = prop.Name;
        if (n.Contains("Xml", StringComparison.OrdinalIgnoreCase) && n.Length > 3)
            return true;
        if (n.Contains("Cautruc", StringComparison.OrdinalIgnoreCase))
            return true;
        if (n.Contains("Noidung", StringComparison.OrdinalIgnoreCase) && n.Length > 6)
            return true;
        return false;
    }

    private static string? FormatValue(object value)
    {
        return value switch
        {
            DateTime dt => dt.ToString("o", CultureInfo.InvariantCulture),
            DateTimeOffset dto => dto.ToString("o", CultureInfo.InvariantCulture),
            Guid g => g.ToString("D"),
            bool b => b ? "true" : "false",
            string s => s,
            IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }
}
