using System;
using System.Linq;
using Localizor.LanguageChangeEvent;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Serialization;

namespace Tutorial;

[Serializable]
public class QuestEntry
{
	public string localizeKey;

	public QuestEntryCustomLocalization customLocalization;

	[SerializeField]
	[Expandable]
	public QuestEntryTarget target;

	[SerializeField]
	[Expandable]
	private HelpSlugOverrider helpSlugOverrider;

	[SerializeField]
	[FormerlySerializedAs("questAffectingChanges")]
	private string[] questTriggers;

	[SerializeField]
	private bool undefinedGameEventTriggersToo = true;

	[SerializeField]
	[Expandable]
	private QuestRequirement[] requirements;

	[SerializeField]
	private TutorialPointerData[] tutorialPointersData;

	[SerializeField]
	private QuestAction[] onCompletedActions;

	[Obsolete("Remove once tutorial has been parsed")]
	[Tooltip("OBSOLETE")]
	public string identifier;

	[NonSerialized]
	private LanguageChangeEventDataHolder _localizationCached;

	public string Id => localizeKey;

	public TutorialPointerData[] TutorialPointersData => tutorialPointersData;

	public string GetForcedHelpPageSlug()
	{
		if (helpSlugOverrider == null)
		{
			return null;
		}
		return helpSlugOverrider.GetTargetHelpSlug();
	}

	public void Init()
	{
		customLocalization?.Init();
	}

	public bool CheckIfCompleted(string change)
	{
		if (requirements.Length == 0)
		{
			if (questTriggers.Length == 0)
			{
				return false;
			}
			if (questTriggers.Length == 1)
			{
				return questTriggers[0] == change;
			}
		}
		if ((undefinedGameEventTriggersToo && string.IsNullOrEmpty(change)) || questTriggers.Length == 0 || questTriggers.Contains(change))
		{
			return requirements.All((QuestRequirement requirement) => requirement != null && requirement.CheckIfCompleted());
		}
		return false;
	}

	public void Complete(bool sideQuest = false)
	{
		if (sideQuest)
		{
			SaveGameManager.Current.completedSideQuestEntries.Add(Id);
		}
		else
		{
			SaveGameManager.Current.CompletedQuestEntries.Add(Id);
		}
		QuestAction[] array = onCompletedActions;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].Do();
		}
		target?.Dispose();
		customLocalization?.Dispose();
	}

	public LanguageChangeEventDataHolder GetLocalisation(bool isSideQuest = false)
	{
		if (string.IsNullOrEmpty(_localizationCached.Key))
		{
			_localizationCached = ((customLocalization != null) ? customLocalization.GetLocalization(localizeKey) : new LanguageChangeEventDataHolder
			{
				Key = localizeKey
			});
		}
		else if (customLocalization != null && customLocalization.IsDynamic() && !IsAlreadyCompleted(isSideQuest))
		{
			_localizationCached = customLocalization.GetLocalization(localizeKey);
		}
		return _localizationCached;
	}

	private bool IsAlreadyCompleted(bool isSideQuest)
	{
		if (!isSideQuest)
		{
			return SaveGameManager.Current.CompletedQuestEntries.Contains(Id);
		}
		return SaveGameManager.Current.completedSideQuestEntries.Contains(Id);
	}
}
