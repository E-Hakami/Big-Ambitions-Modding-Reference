using System;
using System.Collections;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Tutorial;
using UI.Guiders;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Tutorial;

public class TutorialUI : MonoBehaviour
{
	private const string ComingSoonObjectiveId = "tutorial_quest_coming_soon_objective_1";

	[SerializeField]
	private Sprite uncleFredSprite;

	private Transform _uncleFredGroup;

	private bool _showNextQuestAfterTimeMachine;

	private bool _loadingNextQuest;

	private void LateUpdate()
	{
		TutorialHelper.LateUpdate();
	}

	private void OnTimeMachineEnded()
	{
		if (TutorialHelper.IsTutorialEnabled() && _showNextQuestAfterTimeMachine && !_loadingNextQuest)
		{
			StartCoroutine(ShowNextQuestSequence());
			_showNextQuestAfterTimeMachine = false;
		}
	}

	public void Enable()
	{
		GlobalEvents.onTimeMachineEnded = (Action)Delegate.Combine(GlobalEvents.onTimeMachineEnded, new Action(OnTimeMachineEnded));
		TutorialHelper.onQuestLoaded = (Action)Delegate.Combine(TutorialHelper.onQuestLoaded, new Action(OnNewQuestLoaded));
		TutorialHelper.onQuestEntryCompleted = (Action)Delegate.Combine(TutorialHelper.onQuestEntryCompleted, new Action(UpdateQuestEntries));
		_uncleFredGroup = InstanceBehavior<UIs>.Instance.tasksUI.SetUpTasksGroup("uncle_fred");
		if (SaveGameManager.Current.currentWorkoutPlan != null)
		{
			_uncleFredGroup.SetSiblingIndex(1);
		}
		else
		{
			_uncleFredGroup.SetAsFirstSibling();
		}
		if (TutorialHelper.currentQuest == null)
		{
			return;
		}
		UpdateQuestEntries();
		InstanceBehavior<UIs>.Instance.tasksUI.ScheduleUpdateObjectivesHeight();
		base.enabled = true;
		CoroutineUtility.RunAfterSecondsDelay(delegate
		{
			if (!(InstanceBehavior<UIs>.Instance == null) && !InstanceBehavior<UIs>.Instance.monologueUI.IsUp)
			{
				ShowUncleFredMessage();
			}
		}, 3.5f);
	}

	public void Disable()
	{
		GlobalEvents.onTimeMachineEnded = (Action)Delegate.Remove(GlobalEvents.onTimeMachineEnded, new Action(OnTimeMachineEnded));
		TutorialHelper.onQuestLoaded = (Action)Delegate.Remove(TutorialHelper.onQuestLoaded, new Action(OnNewQuestLoaded));
		TutorialHelper.onQuestEntryCompleted = (Action)Delegate.Remove(TutorialHelper.onQuestEntryCompleted, new Action(UpdateQuestEntries));
		UnityEngine.Object.Destroy(_uncleFredGroup.gameObject);
		InstanceBehavior<UIs>.Instance.tasksUI.ScheduleUpdateObjectivesHeight();
		StopAllCoroutines();
		HappinessHelper.RemoveModifier("ba:happinessmodifier_a_fresh_start");
		base.enabled = false;
	}

	private void ShowUncleFredMessage()
	{
		if (ShouldShowCurrentQuest() && !string.IsNullOrEmpty(TutorialHelper.currentQuest.uncleFredMessageType))
		{
			InstanceBehavior<UIs>.Instance.monologueUI.EnqueueMonologue(TutorialHelper.currentQuest.uncleFredMessageType, TutorialHelper.currentQuest.uncleFredAudioClip, uncleFredSprite);
		}
	}

	private IEnumerator ShowNextQuestSequence()
	{
		_loadingNextQuest = true;
		yield return new WaitForSecondsRealtime(2f);
		if (TutorialHelper.IsTutorialFinished())
		{
			Disable();
			yield break;
		}
		InstanceBehavior<UIs>.Instance.tasksUI.PerformSwitchOutAnimation(ShowUncleFredMessage, 2f);
		yield return new WaitForSecondsRealtime(1f);
		_loadingNextQuest = false;
		UpdateQuestEntries();
	}

	private void OnNewQuestLoaded()
	{
		if (!ShouldShowCurrentQuest())
		{
			UpdateQuestEntries();
		}
		else if (InstanceBehavior<UIs>.Instance.timeMachine.isRunning)
		{
			_showNextQuestAfterTimeMachine = true;
		}
		else if (!_loadingNextQuest)
		{
			StartCoroutine(ShowNextQuestSequence());
		}
	}

	public void UpdateQuestEntries()
	{
		if (_loadingNextQuest || !base.transform.gameObject.activeInHierarchy || TutorialHelper.currentQuest == null)
		{
			return;
		}
		_uncleFredGroup.ClearChildren(keepHiddenChildren: false, "UiTemplate");
		bool active = false;
		QuestEntry[] entries = TutorialHelper.currentQuest.entries;
		foreach (QuestEntry questEntry in entries)
		{
			if (ShouldShowQuestEntry(questEntry))
			{
				Transform obj = UnityEngine.Object.Instantiate(InstanceBehavior<UIs>.Instance.tasksUI.taskEntryTemplate, _uncleFredGroup);
				obj.GetLanguageChangeEventByName("Label").SetData(questEntry.GetLocalisation());
				bool isOn = TutorialHelper.HasCompletedObjective(questEntry.Id);
				obj.Find("Checkmark").GetComponent<Toggle>().isOn = isOn;
				obj.Find("DestinationButton").gameObject.SetActive(value: false);
				obj.gameObject.SetActive(value: true);
				active = true;
			}
		}
		_uncleFredGroup.gameObject.SetActive(active);
		InstanceBehavior<UIs>.Instance.tasksUI.ScheduleUpdateObjectivesHeight();
	}

	private static bool ShouldShowCurrentQuest()
	{
		if (TutorialHelper.currentQuest == null)
		{
			return false;
		}
		QuestEntry[] entries = TutorialHelper.currentQuest.entries;
		for (int i = 0; i < entries.Length; i++)
		{
			if (ShouldShowQuestEntry(entries[i]))
			{
				return true;
			}
		}
		return false;
	}

	private static bool ShouldShowQuestEntry(QuestEntry questEntry)
	{
		return questEntry.Id != "tutorial_quest_coming_soon_objective_1";
	}

	private void OnDestroy()
	{
		TutorialPointersManager.UpdateTutorialPointers(null, DirectionGuiderType.MainQuest);
		TutorialHelper.onQuestLoaded = (Action)Delegate.Remove(TutorialHelper.onQuestLoaded, new Action(OnNewQuestLoaded));
		TutorialHelper.onQuestEntryCompleted = (Action)Delegate.Remove(TutorialHelper.onQuestEntryCompleted, new Action(UpdateQuestEntries));
	}
}
