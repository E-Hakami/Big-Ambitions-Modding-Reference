using System;
using Entities;
using NaughtyAttributes;
using UI.Guiders;
using UnityEngine;

namespace Tutorial.SideQuests;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/SideQuest")]
public class SideQuest : ScriptableObject
{
	public string questNameLocalizationKey;

	[BoxGroup("Quest Giver")]
	public string startMessageType;

	[BoxGroup("Quest Giver")]
	public AudioClip startAudioClip;

	[BoxGroup("Quest Giver")]
	public string endMessageType;

	[BoxGroup("Quest Giver")]
	public AudioClip endAudioClip;

	public SideQuest[] completeOnActivation;

	public QuestRequirement[] initiationRequirements;

	public QuestRequirement[] compatCheckRequirements;

	public bool deactivateWhenInitiationRequirementsFail;

	public QuestEntry[] entries;

	public QuestEntry currentQuestEntry;

	private string Id => base.name;

	public bool IsActive()
	{
		return SaveGameManager.Current.activeSideQuestEntries.Contains(Id);
	}

	public bool IsCompleted()
	{
		return SaveGameManager.Current.completedSideQuestEntries.Contains(Id);
	}

	public void Activate()
	{
		if (IsActive())
		{
			return;
		}
		SaveGameManager.Current.activeSideQuestEntries.Add(Id);
		currentQuestEntry = entries[0];
		SideQuest[] array = completeOnActivation;
		foreach (SideQuest sideQuest in array)
		{
			if (sideQuest.IsActive())
			{
				sideQuest.Complete(null, null, null);
			}
		}
	}

	public void Deactivate(bool markCompleted = false)
	{
		if (IsActive())
		{
			SaveGameManager.Current.activeSideQuestEntries.Remove(Id);
			if (markCompleted && !IsCompleted())
			{
				SaveGameManager.Current.completedSideQuestEntries.Add(Id);
			}
			currentQuestEntry = null;
			TutorialPointersManager.UpdateTutorialPointers(null, DirectionGuiderType.SideQuest);
		}
	}

	public void Complete(Contact uncleFredContact, Transform questGroup, Action<string, AudioClip> showUncleFredMessage)
	{
		if (!IsCompleted())
		{
			SaveGameManager.Current.activeSideQuestEntries.Remove(Id);
			SaveGameManager.Current.completedSideQuestEntries.Add(Id);
			if (uncleFredContact != null && !string.IsNullOrEmpty(endMessageType) && endAudioClip != null)
			{
				uncleFredContact.SendMessage(new TextMessage(endMessageType, null, read: true));
				showUncleFredMessage?.Invoke(endMessageType, endAudioClip);
			}
			UiSoundHelper.Play(UiSound.ObjectiveCompleted);
			questGroup?.gameObject.SetActive(value: false);
			TutorialPointersManager.UpdateTutorialPointers(null, DirectionGuiderType.SideQuest);
			GuidersManager.ResetGuider(DirectionGuiderType.SideQuest);
		}
	}

	public bool CheckIfInitiationCompleted()
	{
		if (initiationRequirements == null || initiationRequirements.Length == 0)
		{
			return true;
		}
		QuestRequirement[] array = initiationRequirements;
		for (int i = 0; i < array.Length; i++)
		{
			if (!array[i].CheckIfCompleted())
			{
				return false;
			}
		}
		return true;
	}

	public void CheckCompatCompletion(GameInstance gameInstance)
	{
		if (gameInstance.completedSideQuestEntries == null || gameInstance.completedSideQuestEntries.Contains(Id) || compatCheckRequirements == null || compatCheckRequirements.Length == 0)
		{
			return;
		}
		bool flag = true;
		QuestRequirement[] array = compatCheckRequirements;
		for (int i = 0; i < array.Length; i++)
		{
			if (!array[i].CheckIfCompleted())
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			gameInstance.completedSideQuestEntries.Add(Id);
			if (gameInstance.activeSideQuestEntries.Contains(Id))
			{
				gameInstance.activeSideQuestEntries.Remove(Id);
			}
		}
	}
}
