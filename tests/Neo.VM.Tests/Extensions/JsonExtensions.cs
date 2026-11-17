// Copyright (C) 2015-2026 The Neo Project.
//
// JsonExtensions.cs file belongs to the neo project and is free
// software distributed under the MIT software license, see the
// accompanying file LICENSE in the main directory of the
// repository or http://www.opensource.org/licenses/mit-license.php
// for more details.
//
// Redistribution and use in source and binary forms with or without
// modifications are permitted.

using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Neo.Test.Extensions;

public static class JsonExtensions
{
    private static readonly JsonSerializerOptions _settings = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    /// <summary>
    /// Deserialize json to object
    /// </summary>
    /// <typeparam name="T">Type</typeparam>
    /// <param name="input">Json</param>
    /// <returns>Unit test</returns>
    public static T DeserializeJson<T>(this string input)
    {
        return JsonSerializer.Deserialize<T>(input, _settings);
    }

    /// <summary>
    /// Serialize UT to json
    /// </summary>
    /// <param name="ut">Unit test</param>
    /// <returns>Json</returns>
    public static string ToJson(this object ut)
    {
        if (ut is null) return "null";
        return JsonSerializer.Serialize(ut, ut.GetType(), _settings);
    }
}
