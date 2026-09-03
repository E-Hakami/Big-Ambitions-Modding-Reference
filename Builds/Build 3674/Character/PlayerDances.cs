using Buildings;
using Dancing;
using Helpers;
using PlayerActivity;
using UI;

namespace Character;

public static class PlayerDances
{
	private const float DancingEnergyConsumptionPerMinute = 0.08f;

	private const string DancingEnergySpender = "dancing";

	private static bool IsDancingWithBoost;

	public static bool IsEnabled
	{
		get
		{
			if (InstanceBehavior<UIs>.Instance?.topBar?.playerDancesUI != null)
			{
				return InstanceBehavior<UIs>.Instance.topBar.playerDancesUI.IsDanceButtonInteractable;
			}
			return false;
		}
	}

	public static void Init()
	{
		InstanceBehavior<GameManager>.Instance.playerController.PlayerChangedNavigation.AddListener(StopDancing);
		InstanceBehavior<GameManager>.Instance.playerController.OnPlayerOutOfEnergy.AddListener(StopDancing);
	}

	public static void StartDancing(DanceType danceType)
	{
		InstanceBehavior<GameManager>.Instance.playerController.Character.SetDance(danceType);
		EnergyHelper.AddEnergySpender("dancing", 0.08f);
		StartOwnNightclubBoost();
	}

	public static void StopDancing()
	{
		InstanceBehavior<GameManager>.Instance.playerController.Character.StopDancing();
		EnergyHelper.RemoveEnergySpender("dancing");
		StopOwnNightclubBoost();
	}

	public static void Enable()
	{
		if (PlayerHelper.ItemInstanceInHands == null && string.IsNullOrEmpty(SaveGameManager.Current.ActiveVehicleId) && !InstanceBehavior<GameManager>.Instance.playerController.NavigationDisabled)
		{
			InstanceBehavior<UIs>.Instance.topBar.playerDancesUI?.EnableDances();
		}
	}

	public static void Disable()
	{
		InstanceBehavior<UIs>.Instance.topBar?.playerDancesUI?.DisableDances();
		StopDancing();
	}

	private static void StartOwnNightclubBoost()
	{
		if (!IsDancingWithBoost && BuildingManager.IsInsideBuilding && InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness && !(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.businessTypeName != "ba:businesstype_nightclub"))
		{
			PlayerActivityBalanceConfig dancingBalanceConfig = NightclubBusinessHelper.GetDancingBalanceConfig();
			if (!(dancingBalanceConfig == null))
			{
				HappinessHelper.EnableTemporalHappinessBoost(dancingBalanceConfig.TemporalType, dancingBalanceConfig.FinalType);
				SaveGameManager.Current.timeEnteredTemporalBoost = TimeHelper.Now();
				SaveGameManager.Current.currentActivityHappinessPerHour = dancingBalanceConfig.BoostHoursPerHour;
				IsDancingWithBoost = true;
			}
		}
	}

	private static void StopOwnNightclubBoost()
	{
		if (IsDancingWithBoost)
		{
			PlayerActivityBalanceConfig dancingBalanceConfig = NightclubBusinessHelper.GetDancingBalanceConfig();
			if (dancingBalanceConfig != null)
			{
				HappinessHelper.DisableTemporalHappinessBoost(dancingBalanceConfig.TemporalType, dancingBalanceConfig.FinalType);
			}
			SaveGameManager.Current.timeEnteredTemporalBoost = TimeHelper.Now();
			IsDancingWithBoost = false;
		}
	}
}
