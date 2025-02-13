﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

// ReSharper disable PossibleMultipleEnumeration

namespace Json.Logic.Rules;

/// <summary>
/// Handles the `max` operation.
/// </summary>
[Operator("max")]
[JsonConverter(typeof(MaxRuleJsonConverter))]
public class MaxRule : Rule
{
	internal List<Rule> Items { get; }

	internal MaxRule(Rule a, params Rule[] more)
	{
		Items = new List<Rule> { a };
		Items.AddRange(more);
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
		var items = Items.Select(i => i.Apply(data, contextData))
			.Select(e => new { Type = e.JsonType(), Value = e.Numberify() })
			.ToList();
		var nulls = items.Where(i => i.Value == null);
		if (nulls.Any())
			throw new JsonLogicException($"Cannot find max with {nulls.First().Type}.");

		return items.Max(i => i.Value!.Value);
	}
}

internal class MaxRuleJsonConverter : JsonConverter<MaxRule>
{
	public override MaxRule? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
	{
		var parameters = JsonSerializer.Deserialize<Rule[]>(ref reader, options);

		if (parameters == null || parameters.Length == 0)
			throw new JsonException("The max rule needs an array of parameters.");

		return new MaxRule(parameters[0], parameters.Skip(1).ToArray());
	}

	public override void Write(Utf8JsonWriter writer, MaxRule value, JsonSerializerOptions options)
	{
		writer.WriteStartObject();
		writer.WritePropertyName("max");
		writer.WriteRules(value.Items, options);
		writer.WriteEndObject();
	}
}
