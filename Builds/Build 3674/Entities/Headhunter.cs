using System;
using System.Collections.Generic;
using BigAmbitions.DayNightCycle;
using Buildings.Office.Headquarters;

namespace Entities;

[Serializable]
public class Headhunter : EmployeeInstance
{
	[Obsolete]
	public string[] assignedHRManagers;

	[Obsolete]
	public bool isRecruiting;

	[Obsolete]
	public string skillRecruiting = "ba:skill_customerservice";

	[Obsolete]
	public float skillValueTarget;

	[Obsolete]
	public List<string> dealBreakerTypes;

	[Obsolete]
	public Timestamp nextRecruit;

	[Obsolete]
	public bool automaticallyReplaceOnRetire;

	[Obsolete]
	public bool automaticallyReplaceOnResign;

	[Obsolete]
	public int remainingCandidatesToRecruit = -1;

	[Obsolete]
	public int amountOfCandidatesToRecruitPreference = -1;

	[Obsolete]
	public List<HeadhunterReplacementData> headhunterReplacementDataList = new List<HeadhunterReplacementData>();

	public override void Reset()
	{
		UnAssignWork();
		base.Reset();
	}

	public override void WorkHourly()
	{
		HeadhunterPlan assignedPlanForHeadhunter = HeadhunterHelper.GetAssignedPlanForHeadhunter(id);
		if (assignedPlanForHeadhunter != null)
		{
			assignedPlanForHeadhunter.CheckForRecruiting();
			assignedPlanForHeadhunter.UpdateReplacementStatus();
		}
	}

	public override void UnAssignWork()
	{
		HeadhunterHelper.GetAssignedPlanForHeadhunter(id)?.UnAssignEmployee();
	}
}
