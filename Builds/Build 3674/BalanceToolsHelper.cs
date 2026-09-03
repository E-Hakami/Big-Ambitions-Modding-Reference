using System;
using System.Linq;
using BigAmbitions.Rivals;
using Helpers;
using IngameDebugConsole;

public static class BalanceToolsHelper
{
	[ConsoleMethod("RecalculateAiIncome", "It will recalculate the AI income from last 7 days", new string[] { })]
	public static void RecalculateAiIncome()
	{
		CompetitionHelper.ClearBusinessDefaults();
		foreach (BuildingRegistration buildingRegistration in SaveGameManager.Current.BuildingRegistrations)
		{
			if (!string.IsNullOrEmpty(buildingRegistration.businessOwnerRivalId) && (!(buildingRegistration.BuildingCached.BuildingType != "ba:buildingtype_retail") || !(buildingRegistration.BuildingCached.BuildingType != "ba:buildingtype_office")))
			{
				AiBusinessDefault businessDefault = CompetitionHelper.GetBusinessDefault(buildingRegistration.BusinessName);
				buildingRegistration.scheduleDays = businessDefault.schedule;
				buildingRegistration.cachedAvailableProducts = buildingRegistration.GetListOfItemsForSale().ToList();
				CompetitionHelper.RecalculateRetailPrices(buildingRegistration);
				buildingRegistration.dailyIncomes.Clear();
			}
		}
		foreach (RivalState rivalState in SaveGameManager.Current.rivalStates)
		{
			rivalState.weeklyIncomeHistory.Clear();
		}
		for (int i = TimeHelper.CurrentDay - 14; i <= TimeHelper.CurrentDay; i++)
		{
			foreach (BuildingRegistration buildingRegistration2 in SaveGameManager.Current.BuildingRegistrations)
			{
				if (!string.IsNullOrEmpty(buildingRegistration2.businessOwnerRivalId) && (!(buildingRegistration2.BuildingCached.BuildingType != "ba:buildingtype_retail") || !(buildingRegistration2.BuildingCached.BuildingType != "ba:buildingtype_office")))
				{
					CompetitionHelper.UpdateDailyValuation(buildingRegistration2, i);
				}
			}
			if (i <= TimeHelper.CurrentDay - 7)
			{
				continue;
			}
			foreach (RivalState rivalState2 in SaveGameManager.Current.rivalStates)
			{
				RivalData rivalData = RivalsHelper.GetRivalData(rivalState2.rivalId);
				rivalState2.weeklyIncomeHistory.Add(new Tuple<int, float>(i, rivalData.WeeklyIncome));
			}
		}
	}
}
