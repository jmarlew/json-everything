﻿using System;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Json.More;
using Json.Pointer;

namespace Json.Logic.Rules;

/// <summary>
/// Handles the `missing_some` operation.
/// </summary>
[Operator("missing_some")]
[JsonConverter(typeof(MissingSomeRuleJsonConverter))]
public class MissingSomeRule : Rule
{
	internal Rule RequiredCount { get; }
	internal Rule Components { get; }

	internal MissingSomeRule(Rule requiredCount, Rule components)
	{
		RequiredCount = requiredCount;
		Components = components;
	}

	/// <summary>
	/// Applies the rule to the input data.
	/// </summary>
	/// <param name="data">The input data.</param>
	/// <param name="contextData">
	///     Optional secondary data.  Used by a few operators to pass a secondary
	///     data context to inner operators.
	/// </param>
	/// <returns>The result of the rule.</returns>
	public override JsonNode? Apply(JsonNode? data, JsonNode? contextData = null)
	{
		var requiredCount = RequiredCount.Apply(data, contextData).Numberify();
		var components = Components.Apply(data, contextData);
		if (components is not JsonArray arr)
			throw new JsonLogicException("Expected array of required paths.");

		var expected = arr.SelectMany(e => e.Flatten()).ToList();
		if (expected.Any(e => e is JsonValue v && !v.TryGetValue(out string? _)))
			throw new JsonLogicException("Expected array of required paths.");

		if (data is not JsonObject)
			return expected.ToJsonArray();

		var paths = expected.Cast<JsonValue>().Select(e => e.GetValue<string?>()!)
			.Select(p => new { Path = p, Pointer = JsonPointer.Parse(p == string.Empty ? "" : $"/{p.Replace('.', '/')}") })
			.Select(p =>
			{
				p.Pointer.TryEvaluate(data, out var value);
				return new { Path = p.Path, Value = value };
			})
			.ToList();

		var missing = paths.Where(p => p.Value == null)
			.Select(k => (JsonNode?)k.Path);
		var found = paths.Count(p => p.Value != null);

		if (found < requiredCount)
			return missing.ToJsonArray();

		return new JsonArray();
	}
}

internal class MissingSomeRuleJsonConverter : JsonConverter<MissingSomeRule>
{
	public override MissingSomeRule? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var parameters = JsonSerializer.Deserialize<Rule[]>(ref reader, options);

		if (parameters is not { Length: 2 })
			throw new JsonException("The missing_some rule needs an array with 2 parameters.");

		return new MissingSomeRule(parameters[0], parameters[1]);
	}

	public override void Write(Utf8JsonWriter writer, MissingSomeRule value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyName("missing_some");
		writer.WriteStartArray();
		writer.WriteRule(value.RequiredCount, options);
		writer.WriteRule(value.Components, options);
		writer.WriteEndArray();
		writer.WriteEndObject();
	}
}
