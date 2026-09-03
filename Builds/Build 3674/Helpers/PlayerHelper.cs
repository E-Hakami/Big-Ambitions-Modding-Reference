using System;
using System.Collections;
using System.IO;
using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.GameAnalytics;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using BigAmbitions.SaveSystem;
using BigAmbitions.Tags;
using Blueprints;
using Boats;
using Buildings.Indoors.InteriorDesign;
using Buildings.Retail.Businesses.CinemaTheater;
using Character;
using Entities;
using Extensions;
using IngameDebugConsole;
using Localizor;
using Parking.UndergroundParking;
using Player.HUD.ItemInfoOverlays;
using PlayerActivity;
using UI;
using UI.Smartphone.Apps.Persona;
using UnityEngine;

namespace Helpers;

public static class PlayerHelper
{
	public const float MaxDistanceForEntityInteraction = 0.4f;

	public const int BackUpSaveAge = 70;

	public const string BackUpSaveName = "OldAgeBackUp";

	private const int PermanentZombieWalkingAge = 80;

	private const int DeathAge = 90;

	public static bool playerDead;

	private static EmployeeInstance PlayerEmployeeInstance;

	public static bool IsHoldingItem => ItemInstanceInHands != null;

	public static bool IsUsingVehicle
	{
		get
		{
			if (SaveGameManager.Current != null)
			{
				return !string.IsNullOrEmpty(SaveGameManager.Current.ActiveVehicleId);
			}
			return false;
		}
	}

	public static bool IsHoldingAMop
	{
		get
		{
			if (IsHoldingItem)
			{
				return ItemInHands.HasTag(TagRef.Itemtag.ismop);
			}
			return false;
		}
	}

	public static PlayerController PlayerController => InstanceBehavior<GameManager>.Instance?.playerController;

	public static CharacterData CharacterData => PlayerController?.Character.appearanceSetter.data;

	public static ItemInstance ItemInstanceInHands
	{
		get
		{
			return CharacterData?.itemInHands;
		}
		set
		{
			if (value == null)
			{
				RemoveItemsFromHands();
			}
			else
			{
				AddItemToHands(value);
			}
		}
	}

	public static Item ItemInHands => ItemInstanceInHands?.ItemCached;

	public static bool IsHoldingShoppingBasket => ItemInHands?.HasTag(TagRef.Itemtag.isshoppingcontainer) ?? false;

	public static bool IsHoldingBag => ItemInHands?.HasTag(TagRef.Itemtag.isbag) ?? false;

	public static float CalculateDailyIncome()
	{
		return SaveGameManager.Current.financialSummaries.TakeLast(7).Sum((FinancialSummary x) => x.totalProfit) / 7f;
	}

	public static bool HasPaidForAllItems()
	{
		if (IsUsingVehicle)
		{
			return VehicleHelper.GetCurrentVehicle().cargoInstances.All((CargoInstance cargoInstance) => cargoInstance.paid);
		}
		if (IsHoldingItem)
		{
			return ItemInstanceInHands.cargoInstances.All((CargoInstance cargoInstance) => cargoInstance.paid);
		}
		return true;
	}

	public static void SaveCurrentPosition()
	{
		Transform transform = PlayerController.transform;
		SaveGameManager.Current.LastPlayerPosition = transform.position;
		SaveGameManager.Current.LastPlayerRotation = transform.rotation;
	}

	public static Vector3 GetPosition()
	{
		return PlayerController.transform.position;
	}

	public static Vector3 GetCityPosition()
	{
		if (SubwaySystem.IsRiding)
		{
			return SubwaySystem.CurrentPosition;
		}
		Transform currentInteriorEntrance = GetCurrentInteriorEntrance();
		if (!currentInteriorEntrance)
		{
			return GetPosition();
		}
		return currentInteriorEntrance.position;
	}

	private static Transform GetCurrentInteriorEntrance()
	{
		CityBuildingController cityBuildingController = null;
		if (UndergroundParkingManager.IsInsideParking && (bool)UndergroundParkingManager.currentParkingEntrance)
		{
			cityBuildingController = UndergroundParkingManager.currentParkingEntrance.parentCbc;
		}
		else if (BuildingManager.IsInsideBuilding && !InstanceBehavior<BuildingManager>.Instance.building.IsHamptonsHouse())
		{
			cityBuildingController = InstanceBehavior<BuildingManager>.Instance.cityBuildingController;
		}
		if (!cityBuildingController || cityBuildingController.entranceDoors == null || cityBuildingController.entranceDoors.Length == 0)
		{
			return null;
		}
		return cityBuildingController.entranceDoors[0]?.doorTransform;
	}

	public static bool IsWithinPlayerDistance(EntityController entityController)
	{
		Vector3 closestNavMeshTargetPosition = entityController.GetClosestNavMeshTargetPosition(GetPosition());
		if (closestNavMeshTargetPosition != Vector3.zero)
		{
			return Vector3.SqrMagnitude(closestNavMeshTargetPosition - GetPosition()) < 0.16000001f;
		}
		return false;
	}

	public static string GetItemNameIconPath(string itemName)
	{
		return "ItemIcons/" + itemName.GetIdWithoutType() + ".png";
	}

	public static void Teleport(Transform point)
	{
		PlayerController.Character.navmeshAgent.Warp(point.position);
		PlayerController.transform.rotation = point.rotation;
	}

	public static void IncreasePlayerAge()
	{
		if (!SaveGameManager.Current.gameVariables.disableAging)
		{
			CharacterData.ageInDays++;
			if (!InstanceBehavior<GameManager>.Instance.IsUIDevScene)
			{
				PlayerController.Character.appearanceSetter.UpdateVisualAge();
			}
			if (ShouldDoBackUpSave())
			{
				DoOldAgeBackUp();
			}
			ThirdPersonCharacter.permanentZombieWalking = ShouldEnablePermanentZombieWalking();
			if (ShouldKillThePlayer())
			{
				KillPlayer();
			}
			TrackPlayerAge();
		}
	}

	private static bool ShouldDoBackUpSave()
	{
		int daysByYears = TimeHelper.GetDaysByYears(70f);
		return CharacterData.ageInDays == daysByYears;
	}

	public static bool ShouldEnablePermanentZombieWalking()
	{
		int daysByYears = TimeHelper.GetDaysByYears(80f);
		return CharacterData.ageInDays >= daysByYears;
	}

	public static bool ShouldKillThePlayer()
	{
		int daysByYears = TimeHelper.GetDaysByYears(90f);
		return CharacterData.ageInDays >= daysByYears;
	}

	private static void DoOldAgeBackUp()
	{
		string filename = "old_age_back_up_save_name".Localize(new
		{
			years = 70
		}).ToString();
		filename = FileSystemHelper.MakeValidFilename(filename).Trim();
		string text = Path.Combine(SaveGamePathHelper.GetCharacterFolderPath(SaveGameManager.Current.characterId), "OldAgeBackUp");
		Directory.CreateDirectory(text);
		SaveGameManager.Save(SaveGameManager.SaveType.OldAgeBackUp, filename, text);
	}

	[ConsoleMethod("KillPlayer", "Kills the player instantly, starting the death scene", new string[] { })]
	public static void KillPlayer()
	{
		playerDead = true;
		InstanceBehavior<GameManager>.Instance.StartCoroutine(DeathScene());
	}

	[ConsoleMethod("ForceZombieWalking", "Sets the zombie walking permanent to either true or false", new string[] { })]
	public static void ForceZombieWalking(bool zombieWalking)
	{
		ThirdPersonCharacter.permanentZombieWalking = zombieWalking;
	}

	private static IEnumerator DeathScene()
	{
		if (PlacementSystem.IsInPlacementMode)
		{
			PlacementHelper.CancelPlacementMode();
		}
		if (PlayerActivityUI.IsPanelOpen)
		{
			InstanceBehavior<UIs>.Instance.playerActivityUI.CancelActivity();
		}
		PlayerController.ResetNavigation();
		PlayerController.SetNavigationBlocker(NavigationBlocker.DeathScene);
		PlayerController.Character.ResetZombieState();
		ItemInstanceInHands = null;
		yield return GetAnimator().RunAnimation(AnimationType.Faint);
		GameObject gameObject = GameObject.Find("Cemetery");
		if ((bool)gameObject && gameObject.TryGetComponent<CemeteryDeadScenePlayer>(out var component))
		{
			yield return component.PlayDeadScene();
		}
	}

	public static Animator GetAnimator()
	{
		return PlayerController.Character.animator;
	}

	public static EmployeeInstance GetPlayerEmployeeInstance()
	{
		if (PlayerEmployeeInstance == null)
		{
			PlayerEmployeeInstance = new EmployeeInstance
			{
				satisfaction = 100f,
				characterData = CharacterData
			};
			PlayerEmployeeInstance.characterData.elements = PlayerEmployeeInstance.characterData.elements.Copy();
		}
		return PlayerEmployeeInstance;
	}

	public static bool IsPlayerWorkingInEmployeeStation(string stationId)
	{
		if (PlayerController.Character.CurrentEntityController is ItemController { ItemInstance: not null } itemController)
		{
			return itemController.ItemInstance.id == stationId;
		}
		return false;
	}

	private static void AddItemToHands(ItemInstance itemInstance)
	{
		if (!itemInstance.ItemCached.HasTag(TagRef.Itemtag.isbag))
		{
			PlayerController.HideAccessoryVisuals(AccessoryType.Hand);
		}
		CharacterData.itemInHands = itemInstance;
		InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetItemInstance(itemInstance);
		itemInstance.AddCallToOnItemsInCargoUpdated(OnItemInHandsCargoUpdated);
		TicketEntryBlocker.UpdateBlockersDelayed();
		InstanceBehavior<OverlayManager>.Instance.UpdateDynamicComponents(null, DynamicOverlayUpdateType.StockUpdate | DynamicOverlayUpdateType.CtaUpdate);
		PlayerDances.Disable();
	}

	public static void RemoveItemsFromHands()
	{
		if (IsHoldingItem)
		{
			if (IsHoldingAMop)
			{
				PlayerController.Character.rightHand.GetComponentInChildren<MopController>()?.UnAssignFromPlayer();
				PlayerController.Character.RemoveHandObject();
			}
			ItemInstanceInHands.RemoveCallFromOnItemsInCargoUpdated(OnItemInHandsCargoUpdated);
			TicketEntryBlocker.UpdateBlockersDelayed();
			if (InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.selectedItemInstance == CharacterData.itemInHands)
			{
				InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.Toggle(isEnabled: false);
			}
			PlayerController.Character.SetHandContent(null);
			CharacterData.itemInHands = null;
			InstanceBehavior<OverlayManager>.Instance.UpdateDynamicComponents(null, DynamicOverlayUpdateType.StockUpdate | DynamicOverlayUpdateType.CtaUpdate);
			PlayerDances.Enable();
			PlayerController.ShowHandAccessoryVisualsIfRequired();
		}
	}

	public static void OnItemInHandsCargoUpdated()
	{
		InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.SetupCargoSlots();
		InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.UpdateNameLabel();
		TicketEntryBlocker.UpdateBlockersDelayed();
		if (ItemInHands.HasTag(TagRef.Itemtag.isshoppingcontainer))
		{
			Transform handContent = PlayerController.Character.GetHandContent();
			if (handContent != null)
			{
				handContent.Find("Products1").gameObject.SetActive(value: false);
				handContent.Find("Products2").gameObject.SetActive(value: false);
				float num = (float)ItemInstanceInHands.cargoInstances.Count / (float)ItemInstanceInHands.ItemCached.cargoCapacity;
				if (num > 0f && num <= 0.5f)
				{
					handContent.Find("Products1").gameObject.SetActive(value: true);
				}
				else if (num > 0.5f)
				{
					handContent.Find("Products2").gameObject.SetActive(value: true);
				}
			}
		}
		else if (ItemInHands.HasTag(TagRef.Itemtag.discardcontainerwhenempty) && ItemInstanceInHands.cargoInstances.Count == 0)
		{
			ItemInstanceInHands = null;
		}
	}

	public static bool IsCarryingAnUmbrella()
	{
		return SaveGameManager.Current.accessoriesData.handAccessoryCargoInstance?.ItemCached?.HasTag(TagRef.Itemtag.isumbrella) == true;
	}

	public static bool IsWearingHeadset()
	{
		return SaveGameManager.Current.accessoriesData.headAccessoryCargoInstance?.ItemCached?.HasTag(TagRef.Itemtag.isaudioheadaccessory) == true;
	}

	public static ICargoHolder GetCurrentCargoHolder()
	{
		if (!IsHoldingItem)
		{
			return IsUsingVehicle ? VehicleHelper.GetCurrentVehicle() : null;
		}
		return ItemInstanceInHands;
	}

	public static PersonalWealthData GetPersonalWealth()
	{
		return new PersonalWealthData
		{
			cash = SaveGameManager.Current.Money,
			totalInvestments = SaveGameManager.Current.investmentFunds.SumValues((InvestmentFund x) => x.CurrentValue),
			totalLoans = SaveGameManager.Current.Loans.SumValues((Loan x) => x.remainingAmount),
			totalAssets = GetTotalAssetsWorth()
		};
	}

	private static float GetTotalAssetsWorth()
	{
		float num = SaveGameManager.Current.VehicleInstances.SumValues((VehicleInstance x) => x.VehicleType.price);
		int num2 = SaveGameManager.Current.playerBoats.SumValues((BoatData x) => x.type.GetBoatType().price);
		float num3 = (float)SaveGameManager.Current.realEstate.SumValues((RealEstate x) => x.purchasePrice);
		return num + (float)num2 + num3;
	}

	private static void TrackPlayerAge()
	{
		if (CharacterData.ageInDays % SaveGameManager.Current.gameVariables.daysPerYear == 0 && SaveGameManager.Current.gameVariables.startingAge == 18 && SaveGameManager.Current.gameVariables.daysPerYear == 60)
		{
			GameAnalytics.TrackAgeChange(TimeHelper.GetYearsByDays(CharacterData.ageInDays));
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		playerDead = false;
		PlayerEmployeeInstance = null;
	}
}
