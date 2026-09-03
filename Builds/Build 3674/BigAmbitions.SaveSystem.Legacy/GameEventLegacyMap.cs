using System.Collections.Generic;

namespace BigAmbitions.SaveSystem.Legacy;

public sealed class GameEventLegacyMap : LegacyMapperBase
{
	public override List<string> Keys => new List<string> { "GenericPersonalGoal.triggers", "QuestEntry.questTriggers", "QuestRequirement.ChangesToCheckOn" };

	protected override Dictionary<int, string> Map => new Dictionary<int, string>
	{
		{ 1, "ba:gameevent_foodeaten" },
		{ 2, "ba:gameevent_enteredbuilding" },
		{ 3, "ba:gameevent_playerenergyincreased" },
		{ 4, "ba:gameevent_itemdropped" },
		{ 5, "ba:gameevent_purchasecompleted" },
		{ 6, "ba:gameevent_newjob" },
		{ 7, "ba:gameevent_itemstockedup" },
		{ 8, "ba:gameevent_newloan" },
		{ 9, "ba:gameevent_rentedbuilding" },
		{ 10, "ba:gameevent_quitjob" },
		{ 11, "ba:gameevent_diplomagranted" },
		{ 12, "ba:gameevent_employeehired" },
		{ 13, "ba:gameevent_employeeassigned" },
		{ 14, "ba:gameevent_cleanedfloor" },
		{ 16, "ba:gameevent_changedbusinessopenstate" },
		{ 17, "ba:gameevent_newday" },
		{ 18, "ba:gameevent_changedbuildingregistration" },
		{ 19, "ba:gameevent_newbusiness" },
		{ 20, "ba:gameevent_moneychange" },
		{ 21, "ba:gameevent_workingatemployeestation" },
		{ 22, "ba:gameevent_enteredvehicle" },
		{ 23, "ba:gameevent_newmarketing" },
		{ 24, "ba:gameevent_newimportpartnership" },
		{ 25, "ba:gameevent_marketinsidersortbydemand" },
		{ 26, "ba:gameevent_startedrecruitmentcampaign" },
		{ 27, "ba:gameevent_returnedtothecity" },
		{ 28, "ba:gameevent_completedtaxiride" },
		{ 29, "ba:gameevent_timemachineended" },
		{ 30, "ba:gameevent_purchasedbuilding" },
		{ 31, "ba:gameevent_newdeliverycontract" },
		{ 32, "ba:gameevent_trainingfinished" },
		{ 33, "ba:gameevent_employeeuniformassigned" },
		{ 34, "ba:gameevent_updateddeliverycontract" },
		{ 35, "ba:gameevent_investmentdone" },
		{ 36, "ba:gameevent_itemcargochanged" },
		{ 37, "ba:gameevent_candidatereceived" },
		{ 38, "ba:gameevent_rejuvenationsurgerydone" },
		{ 39, "ba:gameevent_interiorelementschanged" },
		{ 40, "ba:gameevent_calledunclefred" },
		{ 41, "ba:gameevent_activityfinished" },
		{ 42, "ba:gameevent_loanpaid" },
		{ 43, "ba:gameevent_marketinsideropen" },
		{ 44, "ba:gameevent_blueprintordered" },
		{ 45, "ba:gameevent_closedrivalsapp" },
		{ 46, "ba:gameevent_rivalsentmessage" },
		{ 47, "ba:gameevent_undergonemrternitysurgery" },
		{ 48, "ba:gameevent_newhealthinsuranceplanoffer" },
		{ 49, "ba:gameevent_newhour" },
		{ 50, "ba:gameevent_healthinsuranceplanaccepted" },
		{ 51, "ba:gameevent_onfactorymachinerecipechanged" }
	};
}
