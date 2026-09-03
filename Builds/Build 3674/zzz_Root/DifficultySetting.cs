using System.Collections.Generic;
using Player.DifficultySettings;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/DifficultySetting")]
public class DifficultySetting : ScriptableObject
{
	public string key;

	public Difficulty difficulty = Difficulty.Normal;

	[Range(18f, 65f)]
	public int startingAge = 18;

	public int startingMoney = 4200;

	[Range(0f, 100f)]
	public int taxPercentage = 10;

	[Range(30f, 365f)]
	public int daysPerYear = 60;

	[Range(0.7f, 1.3f)]
	public float marketPriceMultiplier = 1f;

	[Range(0.5f, 1.3f)]
	public float employeeHourlySalaryMultiplier = 1f;

	[Range(0.7f, 1.3f)]
	public float bankInterestMultiplier = 1f;

	public bool tutorialEnabled = true;

	[Range(0f, 1.5f)]
	public float rivalsDifficultyMultiplier = 1f;

	[Range(0.1f, 1f)]
	public float baseCustomerPromotionMultiplier = 0.5f;

	[Range(0f, 2f)]
	public float wholesaleUrgentFeeMultiplier = 0.2f;

	[Range(0f, 2f)]
	public float importerUrgentFeeMultiplier = 0.75f;

	[Range(0.1f, 1f)]
	public float exportMultiplier = 0.65f;

	[Range(0.1f, 1f)]
	public float sellingMultiplier = 0.75f;

	[Header("Indicators")]
	public List<DifficultyIndicator> indicators = new List<DifficultyIndicator>();

	public GameVariables ToGameVariables()
	{
		return new GameVariables
		{
			difficulty = difficulty,
			startingAge = startingAge,
			startingMoney = startingMoney,
			taxPercentage = taxPercentage,
			daysPerYear = daysPerYear,
			marketPriceMultiplier = marketPriceMultiplier,
			employeeHourlySalaryMultiplier = employeeHourlySalaryMultiplier,
			bankInterestMultiplier = bankInterestMultiplier,
			tutorialEnabled = tutorialEnabled,
			rivalsDifficultyMultiplier = rivalsDifficultyMultiplier,
			baseCustomerPromotionMultiplier = baseCustomerPromotionMultiplier,
			wholesaleUrgentFeeMultiplier = wholesaleUrgentFeeMultiplier,
			importerUrgentFeeMultiplier = importerUrgentFeeMultiplier,
			exportMultiplier = exportMultiplier,
			sellingMultiplier = sellingMultiplier
		};
	}

	public static DifficultySetting GetDifficultySettings(Difficulty difficulty)
	{
		if (difficulty == Difficulty.Custom)
		{
			return null;
		}
		DifficultySetting[] difficultySettings = InstanceBehavior<GlobalReferences>.Instance.difficultySettings;
		foreach (DifficultySetting difficultySetting in difficultySettings)
		{
			if (difficultySetting.difficulty == difficulty)
			{
				return difficultySetting;
			}
		}
		return null;
	}
}
