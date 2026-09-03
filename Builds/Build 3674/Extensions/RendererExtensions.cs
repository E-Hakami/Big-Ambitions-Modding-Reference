using UnityEngine;

namespace Extensions;

public static class RendererExtensions
{
	public static bool HasMaterialSlot(this Renderer renderer, int materialIndex)
	{
		if (materialIndex >= 0)
		{
			return materialIndex < renderer.sharedMaterials.Length;
		}
		return false;
	}
}
