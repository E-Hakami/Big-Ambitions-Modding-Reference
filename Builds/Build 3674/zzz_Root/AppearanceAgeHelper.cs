using UnityEngine;

public static class AppearanceAgeHelper
{
	private const int MinAge = 18;

	private const int MinAgeForVisualAging = 18;

	private const int MaxAge = 80;

	private const int MaxAgeForVisualAging = 60;

	private const int DefaultDaysPerYear = 60;

	private static readonly int FadePropertyID = Shader.PropertyToID("_Fade");

	private static readonly int MaskColorRedId = Shader.PropertyToID("_MaskColorRed");

	public static void SetRandomAge(this AppearanceSetter appearanceSetter)
	{
		appearanceSetter.data.ageInDays = GetRandomAgeInDays();
	}

	public static int GetRandomAgeInDays()
	{
		int num = ((SaveGameManager.Current == null) ? 60 : SaveGameManager.Current.gameVariables.daysPerYear);
		return Random.Range(18 * num, 80 * num);
	}

	public static void SetAgeInDays(this AppearanceSetter appearanceSetter, int ageInDays)
	{
		appearanceSetter.data.ageInDays = ageInDays;
		appearanceSetter.UpdateVisualAge();
	}

	public static void UpdateVisualAge(this AppearanceSetter appearanceSetter)
	{
		if (appearanceSetter.HeadMesh == null)
		{
			return;
		}
		int num = ((SaveGameManager.Current == null) ? 60 : SaveGameManager.Current.gameVariables.daysPerYear);
		float num2 = (float)appearanceSetter.data.ageInDays / (float)num;
		SkinnedMeshRenderer headMesh = appearanceSetter.HeadMesh;
		Material ageDependingMaterial = appearanceSetter.AgeDependingMaterial;
		if (ageDependingMaterial == null)
		{
			Debug.LogError($"No age material found for age {num2}");
			return;
		}
		headMesh.sharedMaterial = ageDependingMaterial;
		float value = Mathf.Clamp((num2 - 18f) / 42f, 0f, 1f);
		headMesh.material.SetFloat(FadePropertyID, value);
		if (headMesh.material.HasColor(MaskColorRedId))
		{
			headMesh.material.SetColor(MaskColorRedId, appearanceSetter.data.color);
		}
	}
}
