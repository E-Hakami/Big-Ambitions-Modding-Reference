using System.Collections.Generic;
using Entities;
using Extensions;
using Helpers;
using UI;
using UI.Guiders;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;
using UnityEngine.UI;

namespace Tutorial.SideQuests;

public class SideQuestUI : MonoBehaviour
{
	private readonly Dictionary<string, Transform> _questGroups = new Dictionary<string, Transform>();

	private Contact _uncleFredContact;

	[SerializeField]
	private Sprite uncleFredSprite;

	private void Awake()
	{
		SideQuestHelper.onCompletedSideQuestEntry = LoadSideQuest;
		SideQuestHelper.onQuestInitiated = OnQuestInitiated;
		SideQuestHelper.onSideQuestDeactivated = UnloadSideQuest;
	}

	public void Enable()
	{
		_uncleFredContact = Contact.GetContact("uncle_fred", ContactCategoryName.General, "friends_and_family");
		LoadActiveSideQuests();
		base.enabled = true;
	}

	private void LoadActiveSideQuests()
	{
		foreach (SideQuest activeSideQuest in SideQuestHelper.GetActiveSideQuests())
		{
			LoadSideQuest(activeSideQuest);
		}
	}

	public void Disable()
	{
		foreach (Transform value in _questGroups.Values)
		{
			Object.Destroy(value.gameObject);
		}
		_questGroups.Clear();
		InstanceBehavior<UIs>.Instance.tasksUI.ScheduleUpdateObjectivesHeight();
		StopAllCoroutines();
		base.enabled = false;
	}

	private void LateUpdate()
	{
		SideQuestHelper.LateUpdate();
	}

	private void OnQuestInitiated(SideQuest quest)
	{
		if (!(quest == null) && quest.IsActive())
		{
			LoadSideQuest(quest);
			ShowUncleFredMessage(quest.startMessageType, quest.startAudioClip);
			_uncleFredContact.SendMessage(new TextMessage(quest.startMessageType, null, read: true));
		}
	}

	private void LoadSideQuest(SideQuest quest)
	{
		bool flag = false;
		if (!_questGroups.TryGetValue(quest.questNameLocalizationKey, out var value))
		{
			value = InstanceBehavior<UIs>.Instance.tasksUI.SetUpTasksGroup(quest.questNameLocalizationKey);
			_questGroups.Add(quest.questNameLocalizationKey, value);
			value.SetSiblingIndex((SaveGameManager.Current.currentWorkoutPlan == null) ? 1 : 2);
			flag = true;
		}
		bool flag2 = true;
		QuestEntry[] entries = quest.entries;
		foreach (QuestEntry questEntry in entries)
		{
			bool flag3 = SideQuestHelper.HasCompletedObjective(questEntry.Id);
			if (!flag3)
			{
				flag2 = false;
			}
			if (!flag)
			{
				value.Find(questEntry.Id).Find("Checkmark").GetComponent<Toggle>()
					.isOn = flag3;
				continue;
			}
			Transform obj = Object.Instantiate(InstanceBehavior<UIs>.Instance.tasksUI.taskEntryTemplate, value);
			obj.GetLanguageChangeEventByName("Label").SetData(questEntry.GetLocalisation(isSideQuest: true));
			obj.Find("Checkmark").GetComponent<Toggle>().isOn = flag3;
			obj.Find("DestinationButton").gameObject.SetActive(value: false);
			obj.gameObject.name = questEntry.Id;
			obj.gameObject.SetActive(value: true);
		}
		if (quest.currentQuestEntry != null)
		{
			TutorialPointersManager.UpdateTutorialPointers(quest.currentQuestEntry, DirectionGuiderType.SideQuest);
			TutorialHelper.SetQuestTarget(quest.currentQuestEntry, DirectionGuiderType.SideQuest);
		}
		if (flag2)
		{
			quest.Complete(_uncleFredContact, value, ShowUncleFredMessage);
			List<SideQuest> activeSideQuests = SideQuestHelper.GetActiveSideQuests();
			SideQuest sideQuest = ((activeSideQuests.Count > 0) ? activeSideQuests[0] : null);
			if (sideQuest != null)
			{
				LoadSideQuest(sideQuest);
			}
		}
		InstanceBehavior<UIs>.Instance.tasksUI.ScheduleUpdateObjectivesHeight();
	}

	private void ShowUncleFredMessage(string messageKey, AudioClip clip)
	{
		InstanceBehavior<UIs>.Instance.monologueUI.EnqueueMonologue(messageKey, clip, uncleFredSprite);
	}

	private void UnloadSideQuest(SideQuest quest)
	{
		if (!(quest == null) && _questGroups.TryGetValue(quest.questNameLocalizationKey, out var value))
		{
			Object.Destroy(value.gameObject);
			_questGroups.Remove(quest.questNameLocalizationKey);
			InstanceBehavior<UIs>.Instance.tasksUI.ScheduleUpdateObjectivesHeight();
		}
	}

	private void OnDestroy()
	{
		TutorialPointersManager.UpdateTutorialPointers(null, DirectionGuiderType.SideQuest);
	}
}
