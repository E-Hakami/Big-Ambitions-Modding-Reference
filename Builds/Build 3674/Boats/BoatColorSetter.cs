using Data.VehicleColors;
using UnityEngine;

namespace Boats;

public class BoatColorSetter : MonoBehaviour
{
	private static MaterialPropertyBlock _propertyBlock;

	private static readonly int PrimaryColorMask = Shader.PropertyToID("_MaskColorRed");

	private static readonly int SecondaryColorMask = Shader.PropertyToID("_MaskColorGreen");

	private static MaterialPropertyBlock PropertyBlock => _propertyBlock ?? (_propertyBlock = new MaterialPropertyBlock());

	public void SetColor(BoatColor color)
	{
		LOD[] lODs = GetComponent<LODGroup>().GetLODs();
		for (int i = 0; i < lODs.Length; i++)
		{
			Renderer[] renderers = lODs[i].renderers;
			foreach (Renderer obj in renderers)
			{
				obj.GetPropertyBlock(PropertyBlock);
				PropertyBlock.SetColor(PrimaryColorMask, color.primaryColor);
				PropertyBlock.SetColor(SecondaryColorMask, color.secondaryColor);
				obj.SetPropertyBlock(PropertyBlock);
			}
		}
	}
}
