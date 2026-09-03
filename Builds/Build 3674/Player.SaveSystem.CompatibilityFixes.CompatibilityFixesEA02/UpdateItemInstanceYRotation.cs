using BigAmbitions.Items;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA02;

public class UpdateItemInstanceYRotation : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (ItemInstance item in gameInstance.WorldItemsHashSet)
		{
			item.yRotation = ((Quaternion)item.rotation).eulerAngles.y;
		}
	}
}
