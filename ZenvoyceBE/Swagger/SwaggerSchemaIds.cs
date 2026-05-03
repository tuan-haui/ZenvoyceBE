using Zenvoyce.Application.Common.Models;

namespace Zenvoyce.API.Swagger;

/// <summary>
/// Tạo schema ID ngắn gọn cho OpenAPI/NSwag (tránh chuỗi FullName quá dài).
/// </summary>
public static class SwaggerSchemaIds
{
    public static string For(Type type)
    {
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(ApiResponse<>))
            return "ApiResponseOf" + Format(type.GetGenericArguments()[0]);

        return Format(type);
    }

    private static string Format(Type type)
    {
        if (!type.IsGenericType)
            return type.Name;

        var def = type.GetGenericTypeDefinition();
        var args = type.GetGenericArguments();

        if (def == typeof(IReadOnlyCollection<>) || def == typeof(IEnumerable<>) || def == typeof(List<>))
            return "ArrayOf" + Format(args[0]);

        if (def == typeof(PagedResult<>))
            return "PagedResultOf" + Format(args[0]);

        if (type.Name.Contains('`', StringComparison.Ordinal))
        {
            var baseName = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];
            return baseName + string.Join("And", args.Select(Format));
        }

        return type.Name;
    }
}
