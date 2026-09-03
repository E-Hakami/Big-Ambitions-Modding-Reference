using System;
using HGAttributes;
using UnityEngine;

[Serializable]
public class BusinessProduct
{
	[AutocompleteDropdown("Items")]
	public string itemName;

	[Range(0f, 1f)]
	public float impact = 1f;

	public const float ImpactForUnrelatedProducts = 0.025f;
}
