using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Items;
using HGAttributes;
using Helpers;
using JimmysUnityUtilities;
using NaughtyAttributes;
using UI.Notification;
using UnityEngine;

namespace Seasons;

public class ItemWithSeasonalVisualsController : ItemController
{
	[Serializable]
	public struct SpecialAddon(string itemName, int amount)
	{
		[AutocompleteDropdown("Items")]
		public string itemName = itemName;

		public int amount = amount;
	}

	[Header("ItemWithSeasonalVisualsController")]
	[SerializeField]
	[Tooltip("This object will only be visible during the season (or between specified days)")]
	private GameObject seasonalItems;

	[SerializeField]
	private bool providesSpecialGift;

	[SerializeField]
	[ShowIf("providesSpecialGift")]
	[AutocompleteDropdown("Items")]
	private string specialGift;

	[SerializeField]
	[ShowIf("providesSpecialGift")]
	private List<SpecialAddon> specialAddons;

	[Header("Active on dates")]
	[SerializeField]
	private bool useSeasonDates;

	[SerializeField]
	[HideIf("useSeasonDates")]
	private Date startDate;

	[SerializeField]
	[HideIf("useSeasonDates")]
	private Date endDate;

	public bool CanGrabAnyGift(bool playNotifications)
	{
		if (!BuildingManager.IsInsideBuilding || !base.BuildingContext.IsPlayerOwnedBusiness)
		{
			return false;
		}
		DateTime dateTimeNow = DateHelper.GetDateTimeNow();
		if (dateTimeNow.IsEarlierThan(startDate.GetDateTime(startOfDay: true)) || dateTimeNow.IsLaterThan(endDate.GetDateTime(startOfDay: false)))
		{
			return false;
		}
		GameInstance current = SaveGameManager.Current;
		if (current.acquiredSpecialItems == null)
		{
			current.acquiredSpecialItems = new List<string>();
		}
		current = SaveGameManager.Current;
		if (current.acquiredSpecialItemAddons == null)
		{
			current.acquiredSpecialItemAddons = new List<SpecialAddon>();
		}
		bool num = HasReceivedSpecialGift();
		bool flag = HasClaimedAllSpecialAddons();
		if (!num || !flag)
		{
			if (!PlayerHelper.IsHoldingItem)
			{
				return !PlayerHelper.IsUsingVehicle;
			}
			return false;
		}
		if (playNotifications && (PlayerHelper.IsHoldingItem || PlayerHelper.IsUsingVehicle))
		{
			Notifications.ShowError("notification_specialgift_need_empty_hands");
		}
		return false;
	}

	public override void Awake()
	{
		base.Awake();
		if (useSeasonDates)
		{
			Season seasonByName = SeasonHelper.GetSeasonByName(ItemsGetter.GetByName(itemName).season);
			startDate = seasonByName.startDate;
			endDate = seasonByName.endDate;
		}
	}

	private void OnEnable()
	{
		if (PlayerPrefSettings.SeasonalDecorations)
		{
			SetUpSeasonalItem();
		}
	}

	private void SetUpSeasonalItem()
	{
		if (!(seasonalItems == null))
		{
			DateTime dateTimeNow = DateHelper.GetDateTimeNow();
			seasonalItems.SetActive(dateTimeNow.IsLaterThan(startDate.GetDateTime(startOfDay: true)) && dateTimeNow.IsEarlierThan(endDate.GetDateTime(startOfDay: false)));
		}
	}

	public override bool Interact()
	{
		if (!CanGrabAnyGift(playNotifications: true))
		{
			return base.Interact();
		}
		if (!HasReceivedSpecialGift())
		{
			PlayerHelper.ItemInstanceInHands = ItemHelper.InitializeItemInHandsWithCargo(new CargoInstance(specialGift, 1, 0f));
			SaveGameManager.Current.acquiredSpecialItems.Add(specialGift);
		}
		else
		{
			SpecialAddon nextUnclaimedAddon = GetNextUnclaimedAddon();
			if (nextUnclaimedAddon.itemName == null)
			{
				return true;
			}
			Item byName = ItemsGetter.GetByName(nextUnclaimedAddon.itemName);
			int amount = Mathf.Min(nextUnclaimedAddon.amount, byName.boxSize);
			PlayerHelper.ItemInstanceInHands = ItemHelper.InitializeItemInHandsWithCargo(new CargoInstance(nextUnclaimedAddon.itemName, amount, 0f));
			SaveGameManager.Current.acquiredSpecialItemAddons.Add(new SpecialAddon(nextUnclaimedAddon.itemName, amount));
		}
		return true;
	}

	private bool HasReceivedSpecialGift()
	{
		return SaveGameManager.Current.acquiredSpecialItems.Contains(specialGift);
	}

	private bool HasClaimedAllSpecialAddons()
	{
		foreach (SpecialAddon specialAddon in specialAddons)
		{
			if (!HasClaimedAddon(specialAddon))
			{
				return false;
			}
		}
		return true;
	}

	private SpecialAddon GetNextUnclaimedAddon()
	{
		foreach (SpecialAddon specialAddon in specialAddons)
		{
			if (!HasClaimedAddon(specialAddon))
			{
				return specialAddon;
			}
		}
		return default(SpecialAddon);
	}

	private static bool HasClaimedAddon(SpecialAddon addon)
	{
		return SaveGameManager.Current.acquiredSpecialItemAddons.Where((SpecialAddon a) => a.itemName == addon.itemName).Sum((SpecialAddon a) => a.amount) >= addon.amount;
	}
}
