using System;
using System.Collections.Generic;
using UI;
using UI.Guiders;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Tutorial.SideQuests;

public static class SideQuestHelper
{
	public const string BankruptcyQuestId = "SQBankruptcy";

	private const string AddressableLabel = "SideQuests";

	public static Action<SideQuest> onCompletedSideQuestEntry;

	public static Action<SideQuest> onQuestInitiated;

	public static Action<SideQuest> onSideQuestDeactivated;

	private static List<SideQuest> AllQuestsCache = new List<SideQuest>();

	private static readonly HashSet<string> GameEventsToCheck = new HashSet<string>();

	public static List<SideQuest> AllQuests
	{
		get
		{
			if (AllQuestsCache == null || AllQuestsCache.Count == 0)
			{
				ReloadSideQuests();
			}
			return AllQuestsCache ?? new List<SideQuest>();
		}
	}

	private static void OnGameEventTriggered(string gameEvent)
	{
		GameEventsToCheck.Add(gameEvent);
	}

	public static void EnableSideQuests()
	{
		InstanceBehavior<UIs>.Instance.sideQuestUI.Enable();
		GlobalEvents.onTimeMachineEnded = (Action)Delegate.Combine(GlobalEvents.onTimeMachineEnded, new Action(OnTimeMachineEnded));
		GameEvent.onGameEventTriggered = (Action<string>)Delegate.Combine(GameEvent.onGameEventTriggered, new Action<string>(OnGameEventTriggered));
	}

	public static void DisableSideQuests()
	{
		InstanceBehavior<UIs>.Instance.sideQuestUI.Disable();
		foreach (SideQuest allQuest in AllQuests)
		{
			if (allQuest.IsActive())
			{
				allQuest.Complete(null, null, null);
			}
		}
		TutorialPointersManager.UpdateTutorialPointers(null, DirectionGuiderType.SideQuest);
		GuidersManager.ResetGuider(DirectionGuiderType.SideQuest);
		GlobalEvents.onTimeMachineEnded = (Action)Delegate.Remove(GlobalEvents.onTimeMachineEnded, new Action(OnTimeMachineEnded));
		GameEvent.onGameEventTriggered = (Action<string>)Delegate.Remove(GameEvent.onGameEventTriggered, new Action<string>(OnGameEventTriggered));
	}

	public static void LateUpdate()
	{
		if (InstanceBehavior<UIs>.Instance.timeMachine.isRunning || GameManager.isCitySceneBeingUnloaded || GameEventsToCheck.Count == 0)
		{
			return;
		}
		foreach (string item in GameEventsToCheck)
		{
			CheckForSideQuestInitiators(item);
			CheckForCompletedSideQuestObjectives(item);
			CheckForInvalidatedSideQuestInitiations(item);
		}
		GameEventsToCheck.Clear();
	}

	private static void OnTimeMachineEnded()
	{
		CheckForCompletedSideQuestObjectives(string.Empty);
	}

	private static void ReloadSideQuests()
	{
		List<SideQuest> list = new List<SideQuest>();
		foreach (SideQuest item in Addressables.LoadAssetsAsync<SideQuest>("SideQuests", null).WaitForCompletion())
		{
			list.Add(item);
		}
		AllQuestsCache = list;
	}

	public static List<SideQuest> GetActiveSideQuests()
	{
		List<SideQuest> list = new List<SideQuest>();
		foreach (SideQuest allQuest in AllQuests)
		{
			if (allQuest.IsActive())
			{
				list.Add(allQuest);
			}
		}
		return list;
	}

	public static bool HasCompletedObjective(string objectiveId)
	{
		return SaveGameManager.Current.completedSideQuestEntries.Contains(objectiveId);
	}

	private static void CheckForSideQuestInitiators(string changeType)
	{
		foreach (SideQuest allQuest in AllQuests)
		{
			if (allQuest.IsActive() || allQuest.IsCompleted())
			{
				continue;
			}
			bool flag = false;
			QuestRequirement[] initiationRequirements = allQuest.initiationRequirements;
			for (int i = 0; i < initiationRequirements.Length; i++)
			{
				if (ShouldCheckRequirementOnChange(initiationRequirements[i], changeType))
				{
					flag = true;
					break;
				}
			}
			if (flag && AreInitiationRequirementsCompleted(allQuest, changeType))
			{
				allQuest.Activate();
				onQuestInitiated?.Invoke(allQuest);
			}
		}
	}

	private static void CheckForInvalidatedSideQuestInitiations(string changeType)
	{
		foreach (SideQuest activeSideQuest in GetActiveSideQuests())
		{
			if (activeSideQuest.deactivateWhenInitiationRequirementsFail && !AreChangedInitiationRequirementsCompleted(activeSideQuest, changeType))
			{
				activeSideQuest.Deactivate(markCompleted: true);
				onSideQuestDeactivated?.Invoke(activeSideQuest);
			}
		}
	}

	private static bool ShouldCheckRequirementOnChange(QuestRequirement requirement, string changeType)
	{
		List<string> changesToCheckOn = requirement.ChangesToCheckOn;
		if (changesToCheckOn == null || changesToCheckOn.Count == 0)
		{
			return true;
		}
		return changesToCheckOn.Exists((string change) => change == changeType || string.IsNullOrEmpty(change));
	}

	private static bool AreInitiationRequirementsCompleted(SideQuest quest, string changeType)
	{
		QuestRequirement[] initiationRequirements = quest.initiationRequirements;
		for (int i = 0; i < initiationRequirements.Length; i++)
		{
			if (!initiationRequirements[i].CheckIfCompleted(changeType))
			{
				return false;
			}
		}
		return true;
	}

	private static bool AreChangedInitiationRequirementsCompleted(SideQuest quest, string changeType)
	{
		QuestRequirement[] initiationRequirements = quest.initiationRequirements;
		foreach (QuestRequirement questRequirement in initiationRequirements)
		{
			if (ShouldCheckRequirementOnChange(questRequirement, changeType) && !questRequirement.CheckIfCompleted(changeType))
			{
				return false;
			}
		}
		return true;
	}

	private static void CheckForCompletedSideQuestObjectives(string change)
	{
		foreach (SideQuest activeSideQuest in GetActiveSideQuests())
		{
			bool flag = false;
			QuestEntry questEntry = null;
			QuestEntry[] entries = activeSideQuest.entries;
			foreach (QuestEntry questEntry2 in entries)
			{
				if (!HasCompletedObjective(questEntry2.Id))
				{
					if (questEntry2.CheckIfCompleted(change))
					{
						questEntry2.Complete(sideQuest: true);
						flag = true;
					}
					else if (questEntry == null)
					{
						questEntry = questEntry2;
					}
				}
			}
			if (flag)
			{
				onCompletedSideQuestEntry?.Invoke(activeSideQuest);
			}
			if (questEntry != null)
			{
				activeSideQuest.currentQuestEntry = questEntry;
			}
		}
	}

	public static bool IsSideQuestActive(string questId)
	{
		return SaveGameManager.Current.activeSideQuestEntries.Contains(questId);
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		AllQuestsCache = new List<SideQuest>();
		GameEventsToCheck.Clear();
		onCompletedSideQuestEntry = null;
		onQuestInitiated = null;
		onSideQuestDeactivated = null;
	}
}
