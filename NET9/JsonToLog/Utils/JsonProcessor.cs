using FluentResults;
using Serilog;
using System.Text.Json;

namespace JsonToLog.Utils;

public static class JsonProcessor
{
    public static Result<Dictionary<string, object?>> ExtractKeyValues(string jsonString)
    {
        var dictionary = new Dictionary<string, object?>();
        if (string.IsNullOrWhiteSpace(jsonString))
        {
            return Result.Fail("JSON string is null or empty");
        }

        try
        {
            using (JsonDocument document = JsonDocument.Parse(jsonString))
            {
                ProcessJsonElement(document.RootElement, null, dictionary);
            }
            return Result.Ok(dictionary);
        }
        catch (JsonException ex)
        {
            // JSON 파싱 오류 처리
            Log.Logger.Error(ex, jsonString);
            return Result.Fail($"JSON parsing error: {ex.Message}");
        }
    }

    private static void ProcessJsonElement(JsonElement element, string? currentPath,
        Dictionary<string, object?> accumulator)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    string newPath = string.IsNullOrEmpty(currentPath)
                        ? property.Name
                        : $"{currentPath}.{property.Name}";
                    ProcessJsonElement(property.Value, newPath, accumulator);
                }

                break;
            case JsonValueKind.Array:
                int index = 0;
                foreach (JsonElement item in element.EnumerateArray())
                {
                    string newPath = $"{currentPath}[{index++}]";
                    ProcessJsonElement(item, newPath, accumulator);
                }

                // 배열 자체가 비어있거나, 배열의 내용을 하나의 항목으로 저장하고 싶다면 아래 주석 해제
                // if (!element.EnumerateArray().Any())
                // {
                //     accumulator[currentPath ?? "array"] = "[]";
                // }
                break;
            case JsonValueKind.String:
                accumulator[currentPath!] = element.GetString();
                break;
            case JsonValueKind.Number:
                // 숫자는 double로 우선 저장하거나, 필요에 따라 GetInt32, GetInt64, GetDecimal 등으로 분기
                if (element.TryGetDecimal(out decimal decVal))
                    accumulator[currentPath!] = decVal;
                else if (element.TryGetInt64(out long longVal))
                    accumulator[currentPath!] = longVal;
                else
                    accumulator[currentPath!] = element.GetDouble();
                break;
            case JsonValueKind.True:
                accumulator[currentPath!] = true;
                break;
            case JsonValueKind.False:
                accumulator[currentPath!] = false;
                break;
            case JsonValueKind.Null:
                accumulator[currentPath!] = null;
                break;
            case JsonValueKind.Undefined: // JSON 표준에는 없지만 System.Text.Json은 이를 처리할 수 있음
            default:
                // Console.WriteLine($"Unhandled JsonValueKind: {element.ValueKind} at path {currentPath}");
                accumulator[currentPath ?? "unknown"] = element.ToString(); // 안전하게 문자열로 저장
                break;
        }
    }
}