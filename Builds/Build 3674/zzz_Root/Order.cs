using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.Rivals;
using BigAmbitions.SoundSystem;
using BigAmbitions.Tags;
using Buildings.BuildingTypes.Shared.Dirtiness;
using Buildings.Retail.Businesses.CinemaTheater;
using Entities;
using JimmysUnityUtilities;
using Player.HUD.ItemInfoOverlays;
using UnityEngine;

[Serializable]
public class Order
{
	public List<OrderEntry> entries = new List<OrderEntry>();

	public Timestamp timestamp;

	public bool completed;

	public float customerServiceSkill;

	public float cleanliness;

	public List<string> customerDemandTypes = new List<string>();

	public float customerDemandScore;

	public bool Pay(BuildingRegistration buildingRegistration, Vector3 paySoundSource, bool isPlayer, bool onlyForAcceptablePrices = false)
	{
		if (isPlayer)
		{
			float num = entries.Sum((OrderEntry x) => x.price);
			if (num != 0f)
			{
				Dictionary<string, string> data = new Dictionary<string, string> { { "businessName", buildingRegistration.BusinessName } };
				bool num2 = !buildingRegistration.businessOwnerRivalId.IsSpecialRival() && (buildingRegistration.BuildingCached.SpecialService?.hasTaxDeductiblePurchases ?? false);
				TransactionInfo transactionInfo = new TransactionInfo("ba:transaction_itempurchase", data);
				if (num2)
				{
					transactionInfo.SetTaxDeductibleName(buildingRegistration.BusinessName);
				}
				float amount = 0f - num;
				Address address = buildingRegistration.Address;
				if (!GameManager.ChangeMoneySafe(amount, transactionInfo, null, address, force: false, showNotification: true))
				{
					return false;
				}
			}
			if (entries.Exists((OrderEntry x) => ItemsGetter.GetByName(x.itemName).HasTag(TagRef.Itemtag.isticket)))
			{
				SaveGameManager.Current.hasCinemaTheaterTicket = true;
				TicketEntryBlocker.UpdateBlockers();
			}
		}
		entries.ForEach(delegate(OrderEntry x)
		{
			if (x.priceAccceptable || !onlyForAcceptablePrices)
			{
				x.paid = true;
			}
		});
		cleanliness = buildingRegistration.GetCleanliness();
		completed = true;
		InstanceBehavior<SfxManager>.Instance?.PlayAudio(SoundType.PurchaseSuccess, paySoundSource, 1f, isPlayer);
		if (isPlayer)
		{
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				InstanceBehavior<OverlayManager>.Instance.UpdateDynamicComponents(null, DynamicOverlayUpdateType.CtaUpdate);
			});
		}
		return true;
	}

	public void Complete(BuildingRegistration buildingRegistration, float customerService, float orderCleanliness)
	{
		customerServiceSkill = customerService;
		customerDemandScore = OrderHelper.CalculateOrderDemandScore(this, buildingRegistration);
		if (timestamp == null)
		{
			timestamp = TimeHelper.Now();
		}
		completed = true;
		cleanliness = orderCleanliness;
	}

	public void ResetEntries()
	{
		foreach (OrderEntry entry in entries)
		{
			entry.Reset();
		}
	}

	public void AddPaperBagEntry(float wholesalePrice, bool priceAccceptable = true, bool available = true, bool paid = true)
	{
		OrderEntry item = new OrderEntry
		{
			itemName = ItemsGetter.GetRandomBag(),
			priceAccceptable = priceAccceptable,
			available = available,
			paid = paid,
			wholesalePrice = wholesalePrice
		};
		entries.Add(item);
	}
}
