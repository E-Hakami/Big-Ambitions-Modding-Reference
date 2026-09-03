using System;
using System.Linq;
using Entities;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Targets/RecruitmentAgency")]
public class RecruitmentAgencyTargets : QuestEntryTarget
{
	[SerializeField]
	private QuestEntryTarget addressTarget;

	[SerializeField]
	private string skillNameTarget;

	protected override void OnInit()
	{
		GameEvent.onGameEventTriggered = (Action<string>)Delegate.Combine(GameEvent.onGameEventTriggered, new Action<string>(UpdateTarget));
	}

	protected override void OnDispose()
	{
		GameEvent.onGameEventTriggered = (Action<string>)Delegate.Remove(GameEvent.onGameEventTriggered, new Action<string>(UpdateTarget));
	}

	private void UpdateTarget(string gameEvent)
	{
		if (gameEvent == "ba:gameevent_startedrecruitmentcampaign")
		{
			SetTarget();
		}
	}

	public override Address GetAddress()
	{
		if (SaveGameManager.Current.RecruitmentCampaigns.Any((RecruitmentCampaign x) => x.skillRequirement.skillName == skillNameTarget))
		{
			return null;
		}
		return addressTarget.GetAddress();
	}
}
