using System;
using System.Collections.Generic;
using System.Linq;
using Buildings.BuildingTypes.Special;
using Extensions;
using Helpers;

namespace Buildings;

public static class CasinoBusinessHelper
{
	private static readonly string[] SlotMachineChairName = new string[1] { "ba:itemname_casinoslotmachinechair" };

	private static readonly string[] BlackJackTableName = new string[1] { "ba:itemname_casinoblackjacktable" };

	private static readonly string[] RouletteTableName = new string[1] { "ba:itemname_casinoroulettetable" };

	private static List<ItemController> _slotMachineChairs = new List<ItemController>();

	private static List<PlaySpotsManager> _blackjackTablePlaySpotsManagers = new List<PlaySpotsManager>();

	private static List<PlaySpotsManager> _rouletteTablePlaySpotsManagers = new List<PlaySpotsManager>();

	public static void Init()
	{
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, new Action<Address>(LoadItemControllers));
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, new Action<Address>(UnloadItemControllers));
	}

	private static void LoadItemControllers(Address address)
	{
		if (IsCasino(address))
		{
			_slotMachineChairs = InstanceBehavior<BuildingManager>.Instance.GetItemControllersByName(SlotMachineChairName);
			_blackjackTablePlaySpotsManagers = (from x in InstanceBehavior<BuildingManager>.Instance.GetItemControllersByName(BlackJackTableName).OfType<CasinoGameController>()
				select x.playSpotsManager).ToList();
			_rouletteTablePlaySpotsManagers = (from x in InstanceBehavior<BuildingManager>.Instance.GetItemControllersByName(RouletteTableName).OfType<CasinoGameController>()
				select x.playSpotsManager).ToList();
		}
	}

	private static void UnloadItemControllers(Address address)
	{
		if (IsCasino(address))
		{
			_slotMachineChairs.Clear();
			_blackjackTablePlaySpotsManagers.Clear();
			_rouletteTablePlaySpotsManagers.Clear();
		}
	}

	private static bool IsCasino(Address address)
	{
		return BuildingHelper.GetBuildingRegistration(address)?.businessTypeName == "ba:businesstype_casino";
	}

	public static ItemController GetRandomMachineSlotChair()
	{
		if (!IsThereAnAvailableMachineSlotChair())
		{
			return null;
		}
		return _slotMachineChairs.Where((ItemController x) => !x.Occupied).GetRandom();
	}

	public static bool IsThereAnAvailableMachineSlotChair()
	{
		return _slotMachineChairs.Count((ItemController x) => !x.Occupied) > 1;
	}

	public static PlaySpotsManager GetRandomCasinoGamePlaySpotsManager(CasinoGameType casinoGameType)
	{
		if (!IsThereAnAvailableCasinoGamePlaySpot(casinoGameType))
		{
			return null;
		}
		if (casinoGameType == CasinoGameType.Blackjack)
		{
			return _blackjackTablePlaySpotsManagers.Where((PlaySpotsManager x) => x.IsAnySpotAvailable()).GetRandom();
		}
		return _rouletteTablePlaySpotsManagers.Where((PlaySpotsManager x) => x.IsAnySpotAvailable()).GetRandom();
	}

	public static bool IsThereAnAvailableCasinoGamePlaySpot(CasinoGameType casinoGameType)
	{
		return ((casinoGameType == CasinoGameType.Blackjack) ? _blackjackTablePlaySpotsManagers : _rouletteTablePlaySpotsManagers).Sum((PlaySpotsManager x) => x.GetNumberOfFreeSpots()) > 1;
	}
}
