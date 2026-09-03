using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Extensions;
using UnityEngine;

namespace Helpers;

public static class ColorHelper
{
	private static Color32[] _colors;

	private static FieldInfo[] _colorFields;

	public static Color blackTextColor = new Color(0.06640625f, 0.08203125f, 0.09765625f, 1f);

	public static Color32 GetRandomBaseColor()
	{
		if (_colors == null)
		{
			GenerateColorsArray();
		}
		return _colors.GetRandom();
	}

	public static Color32 GetColorByName(string name)
	{
		FieldInfo fieldInfo = GetColorFields().FirstOrDefault((FieldInfo x) => x.Name == name);
		if (fieldInfo == null)
		{
			throw new Exception("Invalid color name");
		}
		return (Color32)fieldInfo.GetValue(InstanceBehavior<GlobalReferences>.Instance.colors);
	}

	public static string GetColorName(Color color)
	{
		return GetColorFields().FirstOrDefault((FieldInfo x) => (Color32)x.GetValue(InstanceBehavior<GlobalReferences>.Instance.colors) == color)?.Name;
	}

	private static void GenerateColorsArray()
	{
		_colors = (from x in typeof(Colors).GetFields()
			select x.GetValue(InstanceBehavior<GlobalReferences>.Instance.colors)).Cast<Color32>().ToArray();
	}

	private static IEnumerable<FieldInfo> GetColorFields()
	{
		return _colorFields ?? (_colorFields = typeof(Colors).GetFields());
	}

	public static Color EvaluateRandom(this Gradient gradient, int seed = -1)
	{
		if (seed == -1)
		{
			return gradient.Evaluate(UnityEngine.Random.value);
		}
		System.Random random = new System.Random(seed);
		return gradient.Evaluate((float)random.NextDouble());
	}

	public static string ToHex(this Color color)
	{
		int num = Mathf.RoundToInt(color.r * 255f);
		int num2 = Mathf.RoundToInt(color.g * 255f);
		int num3 = Mathf.RoundToInt(color.b * 255f);
		int num4 = Mathf.RoundToInt(color.a * 255f);
		return $"#{num:X2}{num2:X2}{num3:X2}{num4:X2}";
	}

	public static string ToHex(this Color32 color32)
	{
		return ((Color)color32).ToHex();
	}

	public static bool Approximately(this Color a, Color b, float tolerance = 0.01f)
	{
		return Mathf.Abs(a.r - b.r) + Mathf.Abs(a.g - b.g) + Mathf.Abs(a.b - b.b) + Mathf.Abs(a.a - b.a) <= tolerance;
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		_colors = null;
		_colorFields = null;
	}
}
