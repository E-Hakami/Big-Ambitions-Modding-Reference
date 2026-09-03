using System.Linq;
using BigAmbitions.Rivals;
using GamePrompt.Runtime.Scripts;

namespace Extensions;

public static class GamePromptHelper
{
	public static void RunHourly()
	{
		GamePromptManager.Instance.SetProperty("BANK_BALANCE", SaveGameManager.Current.Money);
		GamePromptManager.Instance.SetProperty("BUSINESS_COUNT", SaveGameManager.Current.BuildingRegistrations.Count((BuildingRegistration x) => x.RentedByPlayer && x.businessTypeName != "ba:businesstype_empty"));
		GamePromptManager.Instance.SetProperty("EMPLOYEE_COUNT", SaveGameManager.Current.EmployeeInstances.Count);
		GamePromptManager.Instance.SetProperty("ACTIVE_SPECIAL_RIVALS_COUNT", SaveGameManager.Current.specialRivalStates.Count((SpecialRivalState x) => x.isActive && !x.isDefeated));
	}
}
