﻿using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Json.More;

namespace Json.Path;

internal class PropertySelector : SelectorBase
{
	private readonly string? _name;

	public PropertySelector(string? name)
	{
		_name = name;
	}

	protected override IEnumerable<PathMatch> ProcessMatch(PathMatch match)
	{
		if (_name == null)
		{
			switch (match.Value)
			{
				case JsonObject obj:
					foreach (var propPair in obj)
					{
						yield return new PathMatch(propPair.Value, match.Location.AddSelector(new IndexSelector(new[] { (PropertyNameIndex)propPair.Key })));
					}
					break;
				case JsonArray array:
					foreach (var (value, index) in array.Select((v, i) => (v, i)))
					{
						yield return new PathMatch(value, match.Location.AddSelector(new IndexSelector(new[] { (SimpleIndex)index })));
					}
					break;
			}

			yield break;
		}

		if (match.Value is not JsonObject obj2) yield break;

		if (!obj2.TryGetValue(_name, out var prop, out _)) yield break;

		yield return new PathMatch(prop, match.Location.AddSelector(new IndexSelector(new[] { (PropertyNameIndex)_name })));
	}

	public override string ToString()
	{
		return _name == null ? ".*" : $".{_name}";
	}
}