using System.Collections.Generic;
using BigAmbitions.InteriorDesigner.InteriorElements;
using UnityEngine;

namespace InteriorDesign;

public static class InteriorScoreCalculator
{
	private const float PerfectCostPerElement = 100f;

	public static int GetInteriorScorePercentage(List<SerializedInteriorDesign> designs)
	{
		int num = 0;
		int num2 = 0;
		float num3 = 0f;
		foreach (SerializedInteriorDesign design in designs)
		{
			SerializedInteriorDesign.SerializableInteriorMaterial[] materials = design.materials;
			for (int i = 0; i < materials.Length; i++)
			{
				SerializedInteriorDesign.SerializableInteriorMaterial serializableInteriorMaterial = materials[i];
				if (InteriorElementsHelper.PresetsDictionary.TryGetValue(serializableInteriorMaterial.MaterialID, out var value))
				{
					num++;
					if (!(value.price <= 0f))
					{
						num2++;
						num3 += value.price;
					}
				}
			}
		}
		int count = designs.Count;
		float num4 = Mathf.Min(num3 / (float)count * 100f / 100f, 100f);
		float num5 = (float)num2 * 100f / (float)num;
		return Mathf.FloorToInt((num4 + num5) / 2f);
	}
}
