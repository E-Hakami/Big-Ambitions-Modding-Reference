using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.GameAnalytics;
using BigAmbitions.SoundSystem;
using Data.VehicleColors;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using PlayerActivity;
using UI;
using UI.Notification;
using UI.PurchaseVehicle;
using UnityEngine;

namespace Boats;

public class BoatController : EntityController, IPurchasableAsset
{
	[SerializeField]
	private Boat boat;

	[SerializeField]
	private SleepEnvironment sleepEnvironment;

	private string _initialColorName;

	private PointOfInterest _poi;

	public bool IsPlayerOwned => boat.isPlayerOwned;

	public bool IsLuxuryYacht => boat.boatTypeName.GetBoatType().isLuxuryYacht;

	public string BoatTypeKey => boat.boatTypeName.GetLocalizeKey();

	public string GetLocalizeKey()
	{
		return boat.boatTypeName.GetLocalizeKey();
	}

	public float GetPurchasePrice()
	{
		return GetBoatPrice() + GetYearlyMaintenancePrice();
	}

	public string GetInitialColor()
	{
		return boat.data.boatColorName;
	}

	public List<(string key, string value)> GetSpecs()
	{
		(string, string) item = ("common_price", GetBoatPrice().ToShortCurrencyFormat() ?? "");
		(string, string) item2 = ("boat_yearly_maintenance", GetYearlyMaintenancePrice().ToShortCurrencyFormat() ?? "");
		return new List<(string, string)> { item, item2 };
	}

	public List<(string, Color32)> GetColors()
	{
		return InstanceBehavior<GlobalReferences>.Instance.boatColors.Select((BoatColor x) => (name: x.name, primaryColor: x.primaryColor)).ToList();
	}

	public void SetColor(string colorName, bool updateVisuals = true)
	{
		BoatColor boatColor = InstanceBehavior<GlobalReferences>.Instance.boatColors.FirstOrDefault((BoatColor x) => x.name == colorName);
		if (boatColor == null)
		{
			Debug.LogError("Boat color " + colorName + " not found on GlobalReferences.boatColors");
		}
		else
		{
			boat.SetColor(boatColor, updateVisuals);
		}
	}

	public void ResetColor()
	{
		if (!string.IsNullOrEmpty(_initialColorName))
		{
			SetColor(_initialColorName);
		}
	}

	public bool Purchase()
	{
		if (GetPurchasePrice() > SaveGameManager.Current.Money)
		{
			Notifications.ShowInsufficientMoney();
			return false;
		}
		BoatType boatType = boat.boatTypeName.GetBoatType();
		string localization = boat.boatTypeName.GetLocalization();
		Dictionary<string, string> data = new Dictionary<string, string> { { "boatName", localization } };
		TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_boatbought", data);
		TransactionInfo transactionInfo2 = new TransactionInfo("ba:transaction_boatyearlymaintenance", data);
		if (boatType.taxDeductible)
		{
			string localizeKey = boat.boatTypeName.GetLocalizeKey();
			transactionInfo.SetTaxDeductibleName(localizeKey);
			transactionInfo2.SetTaxDeductibleName(localizeKey);
		}
		GameManager.ChangeMoneySafe(0f - GetBoatPrice(), transactionInfo, null, null, force: true);
		GameManager.ChangeMoneySafe(0f - GetYearlyMaintenancePrice(), transactionInfo2, null, null, force: true);
		_initialColorName = boat.data.boatColorName;
		boat.data.nextMaintenanceDay = SaveGameManager.Current.Day + SaveGameManager.Current.gameVariables.daysPerYear;
		SaveGameManager.Current.playerBoats.Add(boat.data);
		boat.isPlayerOwned = true;
		ShowPoi();
		InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.PurchaseSuccess, InstanceBehavior<GameManager>.Instance.playerController.transform.position, 1f, isPlayerCreatedSound: true);
		GameAnalytics.TrackPurchaseVehicle(boat.boatTypeName.ToStringFast(), SaveGameManager.Current.Day);
		return true;
	}

	public void Order(Address deliveryAddress, Contact storeContact, bool showNotification)
	{
	}

	public IEnumerator ShowcaseAnimation()
	{
		InstanceBehavior<UIs>.Instance.playerHUD.purchaseVehicleUI.Close();
		Notifications.Show(NotificationType.Success, "purchasevehicleui_notification_purchase_boat_successful");
		GameEvent.Invoke("ba:gameevent_purchasecompleted");
		PurchaseVehicleUI.runningShowcaseAnimation = false;
		yield return null;
	}

	public IEnumerator CancelShowcaseAnimation()
	{
		yield return null;
	}

	public float GetYearlyMaintenancePrice()
	{
		return GetBoatPrice() * 0.1f;
	}

	public float GetBoatPrice()
	{
		return boat.boatTypeName.GetBoatType().price;
	}

	public override bool Interact()
	{
		if (base.Interact())
		{
			return true;
		}
		if (boat.isPlayerOwned)
		{
			InstanceBehavior<GameManager>.Instance.playerController.ResetWalkingAnimation();
			PlayerActivityUI.Show(sleepEnvironment, this);
		}
		else
		{
			_initialColorName = boat.data.boatColorName;
			InstanceBehavior<UIs>.Instance.playerHUD.purchaseVehicleUI.SetAsset(this);
		}
		return true;
	}

	public void Sell()
	{
		float sellingPrice = GetPurchasePrice() * BoatManager.BoatSellReturn;
		LanguageChangeEventDataHolder bodyData = "boat_sell_confirmation".Localize(new
		{
			boatName = boat.boatTypeName.GetLocalizeKey(),
			price = sellingPrice.ToShortCurrencyFormat()
		});
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			InstanceBehavior<UIs>.Instance.playerActivityUI.CancelActivity();
			SaveGameManager.Current.playerBoats.Remove(boat.data);
			boat.isPlayerOwned = false;
			if ((bool)_poi)
			{
				Object.Destroy(_poi.gameObject);
			}
			Dictionary<string, string> data = new Dictionary<string, string> { 
			{
				"boatTypeName",
				boat.boatTypeName.GetLocalization()
			} };
			TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_boatsold", data);
			GameManager.ChangeMoneySafe(sellingPrice, transactionInfo);
		});
	}

	public void ShowPoi()
	{
		_poi = InstanceBehavior<CityManager>.Instance.cityMap.AddPoi(base.transform, InstanceBehavior<UIs>.Instance.mapFilters.boatIcon, InstanceBehavior<UIs>.Instance.mapFilters.boatFilterColor);
		if ((bool)_poi)
		{
			_poi.SetPermanent();
		}
	}
}
