using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Rivals;
using BigAmbitions.Tags;
using Character.Customization;
using Entities;
using Helpers;
using JimmysUnityUtilities;
using UnityEngine;

namespace UI.Smartphone.Apps.Rivals;

public class RivalLeaderboard : MonoBehaviour
{
	[SerializeField]
	private Transform buttonParent;

	[SerializeField]
	private RivalLeaderboardButton buttonPrefab;

	private readonly List<RivalLeaderboardButton> _buttons = new List<RivalLeaderboardButton>();

	public void Load()
	{
		buttonParent.DestroyAllChildren();
		_buttons.Clear();
		List<RivalLeaderboardData> list = RivalsHelper.GetAllRivalData().Select(GetRivalLeaderboardData).ToList();
		list.Add(GetPlayerLeaderboardData());
		list.Sort((RivalLeaderboardData x, RivalLeaderboardData y) => y.weeklyIncome.CompareTo(x.weeklyIncome));
		for (int num = 0; num < list.Count; num++)
		{
			RivalLeaderboardData rivalLeaderboardData = list[num];
			RivalLeaderboardButton rivalLeaderboardButton = Object.Instantiate(buttonPrefab, buttonParent);
			if (rivalLeaderboardData.rivalId.IsSpecialRival())
			{
				SpecialRivalState specialRivalState = RivalsHelper.GetSpecialRivalState(rivalLeaderboardData.rivalId);
				rivalLeaderboardButton.SetUp(rivalLeaderboardData, this, rivalLeaderboardData.isDefeated ? (-1) : (num + 1), specialRivalState.isActive);
			}
			else
			{
				rivalLeaderboardButton.SetUp(rivalLeaderboardData, this, rivalLeaderboardData.isDefeated ? (-1) : (num + 1));
			}
			_buttons.Add(rivalLeaderboardButton);
			if (num == 0)
			{
				rivalLeaderboardButton.button.onClick.Invoke();
			}
		}
	}

	public void SelectButtonByName(string entryName)
	{
		RivalLeaderboardButton rivalLeaderboardButton = _buttons.FirstOrDefault((RivalLeaderboardButton x) => x.data.entryName == entryName);
		if (rivalLeaderboardButton != null)
		{
			rivalLeaderboardButton.Select();
		}
	}

	public void DeselectButtons(RivalLeaderboardButton selectedButton)
	{
		foreach (RivalLeaderboardButton item in _buttons.Where((RivalLeaderboardButton button) => button != selectedButton))
		{
			item.Deselect();
		}
	}

	private static RivalLeaderboardData GetPlayerLeaderboardData()
	{
		List<BuildingRegistration> list = new List<BuildingRegistration>();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (buildingRegistration.RentedByPlayer && (BusinessTypeHelper.GetData(buildingRegistration).HasTag(TagRef.Businesstag.generatesrevenue) || buildingRegistration.businessTypeName == "ba:businesstype_factory"))
			{
				list.Add(buildingRegistration);
			}
		}
		RivalLeaderboardData rivalLeaderboardData = new RivalLeaderboardData
		{
			ageInYears = TimeHelper.GetYearsByDays(PlayerHelper.CharacterData.ageInDays),
			entryName = PlayerHelper.CharacterData.name,
			weeklyIncome = FinancialSummaryHelper.GetLastFinancialSummaries(7).Sum((FinancialSummary x) => x.totalProfit),
			ownedBusinesses = list,
			ownedBuildings = SaveGameManager.Current.BuildingRegistrations.Where((BuildingRegistration x) => x.BuildingOwnedByPlayer).ToList(),
			isDefeated = false,
			portrait = PortraitGenerator.LoadPlayerPortrait()
		};
		List<BuildingRegistration> ownedBusinesses = rivalLeaderboardData.ownedBusinesses;
		rivalLeaderboardData.mostActiveNeighborhood = ((ownedBusinesses != null && ownedBusinesses.Count > 0) ? (from b in rivalLeaderboardData.ownedBusinesses
			group b by b.Neighborhood into g
			select new
			{
				Neighbourhood = g.Key,
				TotalIncome = g.Sum((BuildingRegistration b) => b.GetAvgWeeklyIncome())
			} into g
			orderby g.TotalIncome descending
			select g).First().Neighbourhood : string.Empty);
		return rivalLeaderboardData;
	}

	public static RivalLeaderboardData GetRivalLeaderboardData(RivalData rival)
	{
		bool isDefeated = false;
		if (rival.WeeklyIncome < 0f || rival.ownedRetailOfficeBusinesses.Count < 1)
		{
			RivalsHelper.DefeatRival(rival);
			isDefeated = true;
		}
		RivalLeaderboardData obj = new RivalLeaderboardData
		{
			rivalId = rival.id,
			entryName = rival.rivalName,
			ageInYears = rival.startingAgeInYears + TimeHelper.GetYearsByDays(SaveGameManager.Current.Day),
			weeklyIncome = rival.WeeklyIncome,
			ownedBusinesses = rival.ownedRetailOfficeBusinesses,
			ownedBuildings = rival.ownedBuildings
		};
		List<BuildingRegistration> ownedRetailOfficeBusinesses = rival.ownedRetailOfficeBusinesses;
		obj.mostActiveNeighborhood = ((ownedRetailOfficeBusinesses != null && ownedRetailOfficeBusinesses.Count > 0) ? rival.MostActiveNeighborhood : string.Empty);
		obj.isDefeated = isDefeated;
		return obj;
	}
}
