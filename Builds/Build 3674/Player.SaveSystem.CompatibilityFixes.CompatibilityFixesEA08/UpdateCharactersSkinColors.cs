using Entities;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA08;

public class UpdateCharactersSkinColors : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		gameInstance.charactersData[0].color = GetColorFromOldSkinColor(gameInstance.charactersData[0].skinColor);
		foreach (EmployeeInstance employeeInstance in gameInstance.EmployeeInstances)
		{
			employeeInstance.characterData.color = GetColorFromOldSkinColor(employeeInstance.characterData.skinColor);
		}
		foreach (EmployeeInstance candidateEmployeeInstance in gameInstance.CandidateEmployeeInstances)
		{
			candidateEmployeeInstance.characterData.color = GetColorFromOldSkinColor(candidateEmployeeInstance.characterData.skinColor);
		}
	}

	public static Color32 GetColorFromOldSkinColor(int oldSkinColorIndex)
	{
		return oldSkinColorIndex switch
		{
			0 => new Color(0.95686275f, 48f / 85f, 0.42745098f, 1f), 
			1 => new Color(0.58431375f, 0.36078432f, 18f / 85f, 1f), 
			2 => new Color(20f / 51f, 0.20784314f, 0.14509805f, 1f), 
			_ => new Color(0.58431375f, 0.36078432f, 18f / 85f, 1f), 
		};
	}
}
