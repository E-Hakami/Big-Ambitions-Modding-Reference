using System.Collections.Generic;
using IngameDebugConsole;
using PlayerActivity;
using UI;
using UI.InteriorDesigner;
using UI.Purchase;
using UI.PurchaseVehicle;
using UnityEngine;

namespace Helpers;

public static class EnergyHelper
{
	public const string AddressableLabel = "EnergySettings";

	private const float NoEnergyWaste = 0f;

	private const EnergyRegen NoEnergyRegen = EnergyRegen.None;

	private const float NoEnergyRegenPerMinute = 0f;

	private static readonly Dictionary<string, float> EnergyWastePerMinute = new Dictionary<string, float>();

	private static EnergyRegen CurrentEnergyRegen = EnergyRegen.None;

	private static EnergySettings EnergySettings;

	private static bool Invincibility;

	private static bool LoggedDefaultSettingsWarning;

	private static float LastEnergyValue;

	private static float LastHungerValue;

	public static float energyRegenMultiplier = 1f;

	public static bool goingToHospital;

	public static float MaxDailyEnergyGeneratedFromConsumables => Settings.MaxDailyEnergyGeneratedFromConsumables;

	public static float DefaultEnergyRegenMultiplier => Settings.DefaultEnergyRegenMultiplier;

	private static float CurrentEnergy => SaveGameManager.Current.Energy;

	private static float CurrentHunger => SaveGameManager.Current.Hunger;

	private static EnergySettings Settings => GetSettings(logDefaultSettingsWarning: true);

	private static float HappinessDecrease => Settings.GetHappinessDecrease(HappinessHelper.Happiness);

	public static float GetEnergyWasteByEnum(this EnergyConsumption consumption)
	{
		return consumption switch
		{
			EnergyConsumption.Minimal => 0.04f, 
			EnergyConsumption.Low => 0.05f, 
			EnergyConsumption.Average => 0.056f, 
			EnergyConsumption.High => 0.25f, 
			_ => 0f, 
		};
	}

	private static float GetEnergyRegenByEnum(this EnergyRegen regen)
	{
		return regen switch
		{
			EnergyRegen.Bed => 0.35f, 
			EnergyRegen.Bench => 0.25f, 
			EnergyRegen.Car => 0.3f, 
			EnergyRegen.Hospital => 0.2f, 
			_ => 0f, 
		};
	}

	public static void Init()
	{
		LastEnergyValue = CurrentEnergy;
		LastHungerValue = CurrentHunger;
		energyRegenMultiplier = Settings.DefaultEnergyRegenMultiplier;
	}

	private static EnergySettings GetSettings(bool logDefaultSettingsWarning)
	{
		if (EnergySettings != null)
		{
			return EnergySettings;
		}
		EnergySettings = ScriptableObject.CreateInstance<EnergySettings>();
		if (logDefaultSettingsWarning && !LoggedDefaultSettingsWarning)
		{
			Debug.LogWarning("EnergySettings has not loaded. Using default runtime settings.");
			LoggedDefaultSettingsWarning = true;
		}
		return EnergySettings;
	}

	public static void SetCurrentEnergyRegen(EnergyRegen value)
	{
		CurrentEnergyRegen = value;
		if (value == EnergyRegen.None)
		{
			InstanceBehavior<GameManager>.Instance.playerController.Character.ClearPlayerExpressionsQueue();
		}
	}

	private static void SetCurrentEnergy(float value)
	{
		SaveGameManager.Current.Energy = value;
		ShowEnergyEmojis(value);
	}

	public static void SetCurrentHunger(float value)
	{
		SaveGameManager.Current.Hunger = value;
		ShowHungerEmojis(value);
	}

	private static void ShowHungerEmojis(float newHungerValue)
	{
		if (!InstanceBehavior<UIs>.Instance.timeMachine.isRunning)
		{
			if (newHungerValue > LastHungerValue)
			{
				ShowPlayerExpression(CharacterEmojiName.PlayerHungerIncrease);
			}
			else if (newHungerValue < Settings.TooLowEnergyHungerExpressionThreshold && LastHungerValue >= Settings.TooLowEnergyHungerExpressionThreshold)
			{
				ShowPlayerExpression(CharacterEmojiName.PlayerHungerTooLow);
			}
			else if (newHungerValue < Settings.LowEnergyHungerExpressionThreshold && LastHungerValue >= Settings.LowEnergyHungerExpressionThreshold)
			{
				ShowPlayerExpression(CharacterEmojiName.PlayerHungerLow);
			}
			LastHungerValue = CurrentHunger;
		}
	}

	private static void ShowEnergyEmojis(float newEnergyValue)
	{
		if (!InstanceBehavior<UIs>.Instance.timeMachine.isRunning)
		{
			if (newEnergyValue > LastEnergyValue)
			{
				ShowPlayerExpression(CharacterEmojiName.PlayerEnergyIncrease);
			}
			else if (newEnergyValue <= Settings.MinEnergyHungerHappinessValue && LastEnergyValue > Settings.MinEnergyHungerHappinessValue)
			{
				ShowPlayerExpression(CharacterEmojiName.PlayerEnergyCritical);
			}
			else if (newEnergyValue < Settings.TooLowEnergyHungerExpressionThreshold && LastEnergyValue >= Settings.TooLowEnergyHungerExpressionThreshold)
			{
				ShowPlayerExpression(CharacterEmojiName.PlayerEnergyTooLow);
			}
			else if (newEnergyValue < Settings.LowEnergyHungerExpressionThreshold && LastEnergyValue >= Settings.LowEnergyHungerExpressionThreshold)
			{
				ShowPlayerExpression(CharacterEmojiName.PlayerEnergyLow);
			}
			LastEnergyValue = newEnergyValue;
		}
	}

	private static void ShowPlayerExpression(CharacterEmojiName characterEmojiName)
	{
		InstanceBehavior<GameManager>.Instance.playerController.Character.EnqueuePlayerExpression(characterEmojiName, Settings.PlayerExpressionDurationSeconds);
	}

	public static void UpdateEnergy(float deltaTimeWithMultiplier)
	{
		if (Invincibility || SaveGameManager.Current.gameVariables.disableEnergy || goingToHospital)
		{
			return;
		}
		float num = 0f;
		foreach (KeyValuePair<string, float> item in EnergyWastePerMinute)
		{
			num += item.Value;
		}
		if (num > 0f)
		{
			SpentEnergyOnce(num * deltaTimeWithMultiplier);
		}
		if (CurrentEnergyRegen != EnergyRegen.None)
		{
			GenerateEnergy(CurrentEnergyRegen.GetEnergyRegenByEnum() * energyRegenMultiplier * deltaTimeWithMultiplier);
		}
		if (GoToHospitalIsEnabled() && Mathf.Approximately(SaveGameManager.Current.Energy, Settings.HospitalizationEnergyThreshold))
		{
			goingToHospital = true;
			InstanceBehavior<GameManager>.Instance.StartCoroutine(InstanceBehavior<GameManager>.Instance.HospitalRespawn());
		}
	}

	private static bool GoToHospitalIsEnabled()
	{
		if (!InstanceBehavior<GameManager>.Instance.IsUIDevScene && !PlayerActivityUI.IsPanelOpen && string.IsNullOrEmpty(SaveGameManager.Current.ActiveVehicleId) && !CasinoBoatManager.IsOnCasinoBoat && !InteriorDesignerUI.IsOpen && !InstanceBehavior<UIs>.Instance.playerHUD.dialogUI.isPanelOpen && !PurchaseUI.IsPanelOpen && !PurchaseVehicleUI.IsPanelOpen && !SubwaySystem.IsRiding)
		{
			return !PlayerHelper.playerDead;
		}
		return false;
	}

	public static void GenerateEnergy(float amount)
	{
		if (!SaveGameManager.Current.gameVariables.disableEnergy)
		{
			if (CurrentEnergy < Settings.MinEnergyHungerHappinessValue)
			{
				SetCurrentEnergy(Settings.MinEnergyHungerHappinessValue);
			}
			SetCurrentEnergy(Mathf.Min(CurrentEnergy + amount - amount * HappinessDecrease, Settings.MaxEnergyHungerHappinessValue));
		}
	}

	public static void SpentEnergyOnce(float amount)
	{
		if (!Invincibility && !SaveGameManager.Current.gameVariables.disableEnergy)
		{
			float currentEnergy = CurrentEnergy;
			float energyWasteMultiplier = Settings.GetEnergyWasteMultiplier(HappinessHelper.Happiness, CurrentHunger);
			SetCurrentHunger(Mathf.Max(CurrentHunger - amount * Settings.HungerSpentPerEnergySpent * (1f + HappinessDecrease), Settings.MinEnergyHungerHappinessValue));
			SetCurrentEnergy(Mathf.Max(CurrentEnergy - amount * energyWasteMultiplier, Settings.HospitalizationEnergyThreshold));
			if (currentEnergy > Settings.MinEnergyHungerHappinessValue && CurrentEnergy < Settings.MinEnergyHungerHappinessValue)
			{
				InstanceBehavior<GameManager>.Instance.playerController.OnPlayerOutOfEnergy.Invoke();
			}
		}
	}

	public static void OnEnergySettingsLoaded(IList<EnergySettings> settings)
	{
		if (settings != null && settings.Count > 0)
		{
			EnergySettings = settings[0];
		}
	}

	public static void SpentEnergyOnce(EnergyConsumption consumption)
	{
		if (!SaveGameManager.Current.gameVariables.disableEnergy && consumption != EnergyConsumption.None)
		{
			SpentEnergyOnce(consumption.GetEnergyWasteByEnum());
		}
	}

	public static void AddEnergySpender(string type, EnergyConsumption consumption)
	{
		if (!SaveGameManager.Current.gameVariables.disableEnergy && !EnergyWastePerMinute.ContainsKey(type))
		{
			EnergyWastePerMinute.Add(type, consumption.GetEnergyWasteByEnum());
		}
	}

	public static void AddEnergySpender(string type, float energyWaste)
	{
		if (!SaveGameManager.Current.gameVariables.disableEnergy)
		{
			EnergyWastePerMinute.TryAdd(type, energyWaste);
		}
	}

	public static void RemoveEnergySpender(string type)
	{
		if (!SaveGameManager.Current.gameVariables.disableEnergy && EnergyWastePerMinute.ContainsKey(type))
		{
			EnergyWastePerMinute.Remove(type);
		}
	}

	[ConsoleMethod("ToggleInvincibility", "Toggles invincibility", new string[] { })]
	public static void Command_ToggleInvincibility()
	{
		Invincibility = !Invincibility;
		Debug.Log("Invincibility: " + Invincibility);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		EnergySettings settings = GetSettings(logDefaultSettingsWarning: false);
		EnergyWastePerMinute.Clear();
		CurrentEnergyRegen = EnergyRegen.None;
		energyRegenMultiplier = settings.DefaultEnergyRegenMultiplier;
		goingToHospital = false;
		LastEnergyValue = settings.MaxEnergyHungerHappinessValue;
		LastHungerValue = settings.MaxEnergyHungerHappinessValue;
		EnergySettings = null;
		LoggedDefaultSettingsWarning = false;
		Invincibility = false;
	}
}
