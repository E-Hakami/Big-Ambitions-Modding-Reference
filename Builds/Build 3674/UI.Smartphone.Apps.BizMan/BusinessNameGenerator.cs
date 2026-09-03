using System;
using NaughtyAttributes;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan;

public class BusinessNameGenerator : ScriptableObject
{
	[Serializable]
	public class Format
	{
		public float chance;

		[Tooltip("Use {brand} for brand, {adjective} for adjective, {regional} for regional alias, and {type} for store type noun.")]
		public string format;
	}

	public Format[] formats;

	[Space]
	[InfoBox("Regional aliases and store type nouns are defined on the NeighborhoodData and BusinessType ScriptableObjects.", EInfoBoxType.Normal)]
	public string[] brands;

	public string[] adjectives;

	[Space]
	public string typeFallbackText;

	public string regionalFallbackText;

	public string GenerateName(BusinessType businessType, string neighborhood)
	{
		return GetFormat().format.Replace("{brand}", GetBrand()).Replace("{adjective}", GetAdjective()).Replace("{regional}", GetRegionalAlias(neighborhood))
			.Replace("{type}", GetStoreTypeNoun(businessType));
	}

	private Format GetFormat()
	{
		float num = 0f;
		Format[] array = formats;
		foreach (Format format in array)
		{
			num += format.chance;
		}
		float num2 = UnityEngine.Random.Range(0f, num);
		array = formats;
		foreach (Format format2 in array)
		{
			if (num2 < format2.chance)
			{
				return format2;
			}
			num2 -= format2.chance;
		}
		return formats[^1];
	}

	private string GetBrand()
	{
		return brands[UnityEngine.Random.Range(0, brands.Length)];
	}

	private string GetAdjective()
	{
		return adjectives[UnityEngine.Random.Range(0, adjectives.Length)];
	}

	private string GetRegionalAlias(string neighborhood)
	{
		string[] array = NeighborhoodHelper.GetData(neighborhood)?.aliases;
		if (array == null || array.Length == 0)
		{
			return regionalFallbackText;
		}
		return array[UnityEngine.Random.Range(0, array.Length)];
	}

	private string GetStoreTypeNoun(BusinessType businessType)
	{
		string[] array = businessType?.aliases;
		if (array == null || array.Length == 0)
		{
			return typeFallbackText;
		}
		return array[UnityEngine.Random.Range(0, array.Length)];
	}
}
