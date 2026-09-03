using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameEvent
{
	public static Action<string> onGameEventTriggered;

	public static readonly HashSet<string> AutosaveTriggers = new HashSet<string>
	{
		"ba:gameevent_foodeaten", "ba:gameevent_enteredbuilding", "ba:gameevent_itemdropped", "ba:gameevent_purchasecompleted", "ba:gameevent_newjob", "ba:gameevent_newloan", "ba:gameevent_rentedbuilding", "ba:gameevent_quitjob", "ba:gameevent_diplomagranted", "ba:gameevent_employeehired",
		"ba:gameevent_employeeassigned", "ba:gameevent_cleanedfloor", "ba:gameevent_changedbusinessopenstate", "ba:gameevent_newbusiness", "ba:gameevent_workingatemployeestation", "ba:gameevent_enteredvehicle", "ba:gameevent_newmarketing", "ba:gameevent_newimportpartnership", "ba:gameevent_startedrecruitmentcampaign", "ba:gameevent_completedtaxiride",
		"ba:gameevent_timemachineended", "ba:gameevent_purchasedbuilding", "ba:gameevent_newdeliverycontract", "ba:gameevent_trainingfinished", "ba:gameevent_employeeuniformassigned", "ba:gameevent_updateddeliverycontract", "ba:gameevent_investmentdone", "ba:gameevent_rejuvenationsurgerydone", "ba:gameevent_interiorelementschanged", "ba:gameevent_calledunclefred",
		"ba:gameevent_activityfinished", "ba:gameevent_loanpaid", "ba:gameevent_blueprintordered", "ba:gameevent_undergonemrternitysurgery", "ba:gameevent_newhealthinsuranceplanoffer", "ba:gameevent_healthinsuranceplanaccepted", "ba:gameevent_onfactorymachinerecipechanged"
	};

	public static void AddAutosaveTrigger(string gameEvent)
	{
		AutosaveTriggers.Add(gameEvent);
	}

	public static void Invoke(string gameEvent)
	{
		onGameEventTriggered?.Invoke(gameEvent);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		onGameEventTriggered = null;
	}
}
