using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Tags;
using Buildings;
using Entities;
using Helpers;
using IngameDebugConsole;
using JimmysUnityUtilities;
using UI;
using UI.Smartphone;
using UnityEngine;

namespace BigAmbitions.Rivals;

[Serializable]
public class RivalTimeline
{
	private struct PlannedEntry(string rivalId, string neighborhood, TimelineEntry entry)
	{
		public readonly string rivalId = rivalId;

		public readonly string neighborhood = neighborhood;

		public readonly TimelineEntry entry = entry;
	}

	private const float MinWeeklyIncomeForSurrender = 2000f;

	public const int PlayerBusinessesForRivalDeactivation = 3;

	[Header("Surrender")]
	public string surrenderMessageKey;

	public AudioClip surrenderAudioClip;

	[Header("Activation")]
	public string activationMessageKey;

	public AudioClip activationAudioClip;

	[Header("Deactivation")]
	public string deactivationMessageKey;

	public AudioClip deactivationAudioClip;

	public List<TimelineEntry> allEntries;

	private readonly Dictionary<Timestamp, PlannedEntry> _plannedEntries = new Dictionary<Timestamp, PlannedEntry>();

	private readonly WaitForSeconds _plannedEntriesTimer = new WaitForSeconds(5f);

	private Coroutine _plannedEntriesCoroutine;

	private SpecialRivalState _specialRivalState;

	public IEnumerator PlannedEntriesCoroutine()
	{
		while (_plannedEntries.Count > 0 && InstanceBehavior<UIs>.Instance != null)
		{
			if (!FullMenu.IsOpen)
			{
				foreach (var (key, plannedEntry2) in _plannedEntries.Where((KeyValuePair<Timestamp, PlannedEntry> kvp) => kvp.Key.IsInThePast()).ToList())
				{
					if (plannedEntry2.entry.IsCompleted)
					{
						_plannedEntries.Remove(key);
						continue;
					}
					if (ActivateRivalDefense(plannedEntry2.entry, plannedEntry2.neighborhood))
					{
						CompleteEntry(plannedEntry2.entry, plannedEntry2.rivalId);
					}
					_plannedEntries.Remove(key);
				}
			}
			yield return _plannedEntriesTimer;
		}
		_plannedEntriesCoroutine = null;
	}

	public void StopPlannedEntriesCoroutine()
	{
		if (_plannedEntriesCoroutine != null)
		{
			CoroutineUtility.StopRunning(_plannedEntriesCoroutine);
		}
	}

	public void Check(SpecialRival rival)
	{
		if (!RivalsHelper.IsFeatureEnabled)
		{
			return;
		}
		_specialRivalState = RivalsHelper.GetSpecialRivalState(rival.rivalData.id);
		if (_specialRivalState.isDefeated)
		{
			return;
		}
		(List<BuildingRegistration>, float) playerValues = GetPlayerValues(rival);
		List<BuildingRegistration> item = playerValues.Item1;
		float item2 = playerValues.Item2;
		int count = item.Count;
		if (count > 0)
		{
			bool entranceMessageSent = RivalsHelper.HasMessageBeenSent(rival.rivalData.id, rival.entranceMessageKey);
			CheckEntranceMessage(rival, count, entranceMessageSent);
			CheckActivation(rival, count, entranceMessageSent);
			if (_specialRivalState.isActive && !CheckSurrender(rival.rivalData))
			{
				CheckTriggers(rival, count, item2);
			}
		}
	}

	private void CheckEntranceMessage(SpecialRival rival, int numBusinesses, bool entranceMessageSent)
	{
		if (!entranceMessageSent && numBusinesses >= 1)
		{
			RivalsHelper.SendEntranceMessage(rival);
		}
	}

	private bool CheckSurrender(RivalData rivalData)
	{
		if (rivalData.ownedRetailOfficeBusinesses.Count > 0 && !(rivalData.WeeklyIncome < 2000f))
		{
			return false;
		}
		SpecialRivalState specialRivalState = _specialRivalState;
		if (specialRivalState != null && specialRivalState.isDefeated)
		{
			return true;
		}
		Surrender(rivalData);
		return true;
	}

	private void Surrender(RivalData rivalData, bool sendMessage = true)
	{
		_plannedEntries.Clear();
		if (sendMessage)
		{
			RivalsHelper.SendMessageToPlayerDelayed(rivalData.id, surrenderMessageKey, surrenderAudioClip, delegate
			{
				RivalsHelper.DefeatRival(rivalData);
			});
		}
		else
		{
			RivalsHelper.DefeatRival(rivalData);
		}
	}

	private void CheckActivation(SpecialRival rival, int playerBusinesses, bool entranceMessageSent)
	{
		if (!entranceMessageSent || _specialRivalState == null)
		{
			return;
		}
		if (_specialRivalState.isActive && playerBusinesses < 3)
		{
			RivalsHelper.SendMessageToPlayerDelayed(rival, deactivationMessageKey, deactivationAudioClip, delegate
			{
				DeactivateRival(rival);
			});
		}
		else if (!_specialRivalState.isActive && playerBusinesses >= 3)
		{
			RivalsHelper.SendMessageToPlayerDelayed(rival, activationMessageKey, activationAudioClip, delegate
			{
				ActivateRival(rival);
			});
		}
	}

	private void ActivateRival(SpecialRival rival)
	{
		if (!_specialRivalState.isActive)
		{
			_specialRivalState.isActive = true;
			rival.GetRivalContact().SendMessage(new TextMessage("ba:messagetype_rivalry_activated", null, RivalsHelper.HasMessageBeenSent(rival.rivalData.id, activationMessageKey), isNewInteraction: false, isSpecialMessage: true));
		}
	}

	private void DeactivateRival(SpecialRival rival)
	{
		if (_specialRivalState.isActive)
		{
			_specialRivalState.isActive = false;
			rival.GetRivalContact().SendMessage(new TextMessage("ba:messagetype_rivalry_deactivated", null, RivalsHelper.HasMessageBeenSent(rival.rivalData.id, deactivationMessageKey), isNewInteraction: false, isSpecialMessage: true));
		}
	}

	private void CheckTriggers(SpecialRival rival, int playerBusinesses, float playerWeeklyIncome)
	{
		List<TimelineEntry> plannedEntries = _plannedEntries.Values.Select((PlannedEntry x) => x.entry).ToList();
		float weeklyIncomePercentage = playerWeeklyIncome / rival.rivalData.WeeklyIncome * 100f;
		List<TimelineEntry> list = allEntries.Where((TimelineEntry x) => !x.IsCompleted && playerBusinesses >= x.businesses && weeklyIncomePercentage >= (float)x.weeklyIncomePercentage && !plannedEntries.Contains(x)).ToList();
		if (list.Count < 1)
		{
			return;
		}
		foreach (TimelineEntry item in list)
		{
			Timestamp timestamp = TimeHelper.Now();
			int hour = timestamp.Hour;
			if (hour >= 8)
			{
				if (hour >= 17)
				{
					timestamp.AddDays(1);
					timestamp.Hour = UnityEngine.Random.Range(8, 11);
					timestamp.Minute = UnityEngine.Random.Range(0, 59);
				}
				else
				{
					timestamp.AddMinutes(UnityEngine.Random.Range(10, 40));
				}
			}
			else
			{
				timestamp.Hour = UnityEngine.Random.Range(8, 11);
				timestamp.Minute = UnityEngine.Random.Range(0, 59);
			}
			_plannedEntries.Add(timestamp, new PlannedEntry(rival.rivalData.id, rival.primaryNeighborhood, item));
			_plannedEntriesCoroutine = CoroutineUtility.Run(PlannedEntriesCoroutine());
		}
	}

	private void CompleteEntry(TimelineEntry entry, string rivalId)
	{
		RivalsHelper.SendMessageToPlayer(rivalId, entry.messageLocalizationKey, entry.messageClip, delegate
		{
			if (!RivalDefenseHelper.SpecialMessagesTmpQueue.IsEmpty())
			{
				TextMessage textMessage = RivalDefenseHelper.SpecialMessagesTmpQueue.Dequeue();
				textMessage.read = entry.messageClip != null;
				RivalsHelper.GetSpecialRival(rivalId).GetRivalContact().SendMessage(textMessage);
			}
		});
		SpecialRivalState specialRivalState = _specialRivalState;
		if (specialRivalState.completedTimelineEntryIds == null)
		{
			specialRivalState.completedTimelineEntryIds = new List<string>();
		}
		_specialRivalState.completedTimelineEntryIds.Add(entry.id);
	}

	private bool ActivateRivalDefense(TimelineEntry entry, string neighborhood)
	{
		return entry.defense switch
		{
			DefensiveMechanic.PriceReduction => RivalDefenseHelper.ActivatePriceReduction(neighborhood, entry.aggression), 
			DefensiveMechanic.LowDemand => RivalDefenseHelper.ActivateLowDemand(neighborhood, entry.aggression), 
			DefensiveMechanic.HireBestEmployees => RivalDefenseHelper.ActivateHireEmployees(neighborhood, entry.aggression), 
			_ => false, 
		};
	}

	public void PrintDebugValues(SpecialRival rival)
	{
		(List<BuildingRegistration>, float) playerValues = GetPlayerValues(rival);
		List<BuildingRegistration> item = playerValues.Item1;
		float item2 = playerValues.Item2;
		int count = item.Count;
		float num = item2 / rival.rivalData.WeeklyIncome * 100f;
		Debug.Log($"Player values - Businesses: {count} | Weekly income: {item2} ");
		Debug.Log($"Rival values - Businesses {rival.rivalData.ownedBusinesses.Count} | Weekly income: {rival.rivalData.WeeklyIncome}");
		Debug.Log($"Other values - Weekly income percentage: {num}%");
	}

	public (List<BuildingRegistration>, float) GetPlayerValues(SpecialRival rival)
	{
		List<BuildingRegistration> list = new List<BuildingRegistration>();
		list.Clear();
		float num = 0f;
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer && !(buildingRegistration.Neighborhood != rival.primaryNeighborhood))
			{
				BusinessType data = BusinessTypeHelper.GetData(buildingRegistration);
				if (!(data == null) && data.HasTag(TagRef.Businesstag.generatesrevenue) && data.HasTag(TagRef.Businesstag.allowplayercreation) && BuildingTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Buildingtypetag.showinbusinesslist) && !(buildingRegistration.businessTypeName == "ba:businesstype_headquarters") && buildingRegistration.orderHistory.Any((OrderHistoryEntry orderHistory) => orderHistory.dayNumber > SaveGameManager.Current.Day - 7 && orderHistory.itemSales.Any((OrderHistoryEntry.ItemReport itemSale) => itemSale.amountSold > 0)))
				{
					list.Add(buildingRegistration);
					num += buildingRegistration.GetAvgWeeklyIncome();
				}
			}
		}
		return (list, num);
	}

	[ConsoleMethod("DefeatRival", "Defeat a rival", new string[] { }, AutoCompleteMap = new string[] { "neighborhood=Neighborhoods" })]
	public static void DefeatRival(string neighborhood, bool showMessage = true)
	{
		SpecialRival specialRivalByNeighborhood = RivalsHelper.GetSpecialRivalByNeighborhood(neighborhood);
		specialRivalByNeighborhood.timeline.Surrender(specialRivalByNeighborhood.rivalData, showMessage);
	}

	[ConsoleMethod("DefeatAllRivals", "Defeat all rivals", new string[] { })]
	public static void DefeatAllRivals()
	{
		foreach (SpecialRival specialRival in RivalsHelper.GetSpecialRivals())
		{
			RivalsHelper.DefeatRival(specialRival.rivalData);
		}
		foreach (RivalData nonSpecialRival in RivalsHelper.GetNonSpecialRivals())
		{
			RivalsHelper.DefeatRival(nonSpecialRival);
		}
	}

	[ConsoleMethod("IntroduceRival", "Makes rival send their entrance message", new string[] { }, AutoCompleteMap = new string[] { "neighborhood=Neighborhoods" })]
	public static void IntroduceRival(string neighborhood)
	{
		SpecialRival specialRivalByNeighborhood = RivalsHelper.GetSpecialRivalByNeighborhood(neighborhood);
		if (specialRivalByNeighborhood == null)
		{
			Debug.LogError("No special rival found for neighborhood " + neighborhood);
		}
		else if (RivalsHelper.HasMessageBeenSent(specialRivalByNeighborhood.rivalData.id, specialRivalByNeighborhood.entranceMessageKey))
		{
			Debug.LogWarning("Entrance message already sent for rival " + specialRivalByNeighborhood.rivalData.id);
		}
		else
		{
			RivalsHelper.SendEntranceMessage(specialRivalByNeighborhood);
		}
	}

	[ConsoleMethod("ActivateRivalry", "Activates a rivalry for a special rival", new string[] { }, AutoCompleteMap = new string[] { "neighborhood=Neighborhoods" })]
	public static void ActivateRivalry(string neighborhood)
	{
		SpecialRival specialRivalByNeighborhood = RivalsHelper.GetSpecialRivalByNeighborhood(neighborhood);
		if (specialRivalByNeighborhood == null)
		{
			Debug.LogError("No special rival found for neighborhood " + neighborhood);
			return;
		}
		SpecialRivalState specialRivalState = RivalsHelper.GetSpecialRivalState(specialRivalByNeighborhood.rivalData.id);
		if (specialRivalState == null)
		{
			Debug.LogError("Rival has not introduced themselves yet: " + specialRivalByNeighborhood.rivalData.id);
		}
		else if (specialRivalState.isActive)
		{
			Debug.LogWarning("Rivalry already active for rival " + specialRivalByNeighborhood.rivalData.id);
		}
		else
		{
			specialRivalByNeighborhood.timeline.ActivateRival(specialRivalByNeighborhood);
		}
	}
}
