using NaughtyAttributes;
using UnityEngine;

namespace Helpers;

[CreateAssetMenu(fileName = "EnergySettings", menuName = "BigAmbitions/EnergySettings")]
public class EnergySettings : ScriptableObject
{
	[BoxGroup("Consumables")]
	[Tooltip("Maximum energy that consumables can generate in one day.")]
	[SerializeField]
	private float maxDailyEnergyGeneratedFromConsumables = 30f;

	[BoxGroup("Regeneration")]
	[Tooltip("Default multiplier applied to all energy regeneration before activities override it.")]
	[SerializeField]
	private float defaultEnergyRegenMultiplier = 1f;

	[BoxGroup("Stats")]
	[Tooltip("Maximum value used for normalized energy, hunger, and happiness calculations.")]
	[SerializeField]
	private float maxEnergyHungerHappinessValue = 100f;

	[BoxGroup("Stats")]
	[Tooltip("Minimum value used for normalized energy, hunger, and happiness calculations.")]
	[SerializeField]
	private float minEnergyHungerHappinessValue;

	[BoxGroup("Stats")]
	[Tooltip("Lowest energy value the player can reach before hospitalization starts.")]
	[SerializeField]
	private float hospitalizationEnergyThreshold = -20f;

	[BoxGroup("Feedback")]
	[Tooltip("How long player energy and hunger expression emojis remain queued.")]
	[SerializeField]
	private float playerExpressionDurationSeconds = 2f;

	[BoxGroup("Feedback")]
	[Tooltip("Threshold below which hunger and energy trigger the too-low expression.")]
	[SerializeField]
	private float tooLowEnergyHungerExpressionThreshold = 5f;

	[BoxGroup("Feedback")]
	[Tooltip("Threshold below which hunger and energy trigger the low expression.")]
	[SerializeField]
	private float lowEnergyHungerExpressionThreshold = 25f;

	[BoxGroup("Hunger")]
	[Tooltip("Hunger removed for every point of normal energy spent.")]
	[SerializeField]
	private float hungerSpentPerEnergySpent = 1.5f;

	[BoxGroup("Happiness")]
	[Tooltip("Maximum fractional penalty low happiness applies to energy regeneration and hunger drain.")]
	[SerializeField]
	private float maxHappinessPenaltyAtZeroHappiness = 0.25f;

	[BoxGroup("Energy Burn")]
	[Tooltip("Maximum extra energy burn from low happiness. 0.25 means 25% extra burn at 0 happiness.")]
	[SerializeField]
	private float maxEnergyBurnIncreaseAtZeroHappiness = 0.25f;

	[BoxGroup("Energy Burn")]
	[Tooltip("Maximum extra energy burn from low hunger. 0.5 means 50% extra burn at 0 hunger.")]
	[SerializeField]
	private float maxEnergyBurnIncreaseAtZeroHunger = 0.5f;

	public float MaxDailyEnergyGeneratedFromConsumables => maxDailyEnergyGeneratedFromConsumables;

	public float DefaultEnergyRegenMultiplier => defaultEnergyRegenMultiplier;

	public float MaxEnergyHungerHappinessValue => maxEnergyHungerHappinessValue;

	public float MinEnergyHungerHappinessValue => minEnergyHungerHappinessValue;

	public float HospitalizationEnergyThreshold => hospitalizationEnergyThreshold;

	public float PlayerExpressionDurationSeconds => playerExpressionDurationSeconds;

	public float TooLowEnergyHungerExpressionThreshold => tooLowEnergyHungerExpressionThreshold;

	public float LowEnergyHungerExpressionThreshold => lowEnergyHungerExpressionThreshold;

	public float HungerSpentPerEnergySpent => hungerSpentPerEnergySpent;

	public float GetEnergyWasteMultiplier(float happiness, float hunger)
	{
		return (1f + maxEnergyBurnIncreaseAtZeroHappiness * (1f - Mathf.Clamp01(happiness / maxEnergyHungerHappinessValue))) * (1f + maxEnergyBurnIncreaseAtZeroHunger * (1f - Mathf.Clamp01(hunger / maxEnergyHungerHappinessValue)));
	}

	public float GetHappinessDecrease(float happiness)
	{
		return maxHappinessPenaltyAtZeroHappiness * ((maxEnergyHungerHappinessValue - happiness) / maxEnergyHungerHappinessValue);
	}
}
