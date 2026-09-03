using System.Collections.Generic;
using Extensions;
using IngameDebugConsole;
using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace UI.Smartphone.Apps.Persona;

public class PersonalGoalsUI : MonoBehaviour
{
	private enum SortCategory
	{
		None,
		CurrentGoalTier,
		SteamAchievementTier
	}

	private enum SortDirection
	{
		None,
		Descending,
		Ascending
	}

	private static readonly Vector3 AscendingSortArrowRotation = Vector3.zero;

	private static readonly Vector3 DescendingSortArrowRotation = new Vector3(0f, 0f, 180f);

	[SerializeField]
	private Transform personalGoalTemplate;

	[SerializeField]
	private TextLocalizationComponent personalGoalsHeaderLabel;

	[SerializeField]
	private UiOrder uiOrder;

	[SerializeField]
	private GameObject currentGoalTierSortArrow;

	[SerializeField]
	private GameObject steamAchievementTierSortArrow;

	private readonly List<PersonalGoalTierGroup> _goalTierGroups = new List<PersonalGoalTierGroup>();

	private readonly List<Transform> _goalEntries = new List<Transform>();

	private readonly List<PersonalGoalTierGroup> _defaultGoalTierGroups = new List<PersonalGoalTierGroup>();

	private readonly List<Transform> _defaultGoalEntries = new List<Transform>();

	private SortCategory _sortCategory;

	private SortDirection _sortDirection;

	public static void UpdatePersonalGoals(string trigger)
	{
		List<GenericPersonalGoal> personalGoals = InstanceBehavior<GameManager>.Instance.personalGoals;
		for (int i = 0; i < personalGoals.Count; i++)
		{
			GenericPersonalGoal genericPersonalGoal = personalGoals[i];
			if (IsValidPersonalGoal(genericPersonalGoal, trigger))
			{
				genericPersonalGoal.CheckForCompletion();
			}
		}
	}

	private static bool IsValidPersonalGoal(GenericPersonalGoal personalGoal, string trigger)
	{
		if (SaveGameManager.Current.completedPersonalGoals.Contains(personalGoal.identifier))
		{
			return false;
		}
		if (string.IsNullOrEmpty(trigger))
		{
			return personalGoal.triggers.Count == 0;
		}
		return personalGoal.triggers.Contains(trigger);
	}

	private void OnEnable()
	{
		RefreshSortArrows();
		SetUpPersonalGoals();
	}

	private void Start()
	{
		InstanceBehavior<PersonalGoalOverlay>.Instance.onGoalCompleted.AddListener(SetUpPersonalGoals);
	}

	public void ToggleCurrentGoalTierSorting()
	{
		ToggleSorting(SortCategory.CurrentGoalTier);
	}

	public void ToggleSteamAchievementTierSorting()
	{
		ToggleSorting(SortCategory.SteamAchievementTier);
	}

	private void SetUpPersonalGoals()
	{
		personalGoalTemplate.ResetTemplate();
		_goalTierGroups.Clear();
		_goalEntries.Clear();
		_defaultGoalTierGroups.Clear();
		_defaultGoalEntries.Clear();
		List<GenericPersonalGoal> list = InstanceBehavior<GameManager>.Instance.personalGoals.FindAll((GenericPersonalGoal x) => !x.isHidden);
		list.Sort(delegate(GenericPersonalGoal goalA, GenericPersonalGoal goalB)
		{
			int num5 = uiOrder.tierIdentifiers.IndexOf(GetTierIdentifier(goalA));
			int num6 = uiOrder.tierIdentifiers.IndexOf(GetTierIdentifier(goalB));
			return ((num5 == -1) ? 99999 : num5).CompareTo((num6 == -1) ? 99999 : num6);
		});
		List<PersonalGoalTierGroup> goalTierGroups = GetGoalTierGroups(list);
		for (int num = 0; num < goalTierGroups.Count; num++)
		{
			_goalTierGroups.Add(goalTierGroups[num]);
			_defaultGoalTierGroups.Add(goalTierGroups[num]);
		}
		int num2 = 0;
		for (int num3 = 0; num3 < _goalTierGroups.Count; num3++)
		{
			if (_goalTierGroups[num3].AreAllTiersCompleted)
			{
				num2++;
			}
		}
		string progress = $"{num2}/{_goalTierGroups.Count}";
		personalGoalsHeaderLabel.SetData(LanguageChangeEventDataHolder.Create("persona_personal_goals_header", new { progress }));
		for (int num4 = 0; num4 < _goalTierGroups.Count; num4++)
		{
			SetUpPersonalGoal(_goalTierGroups[num4]);
		}
		ApplySorting();
	}

	private static List<PersonalGoalTierGroup> GetGoalTierGroups(List<GenericPersonalGoal> personalGoals)
	{
		List<PersonalGoalTierGroup> list = new List<PersonalGoalTierGroup>();
		Dictionary<string, PersonalGoalTierGroup> dictionary = new Dictionary<string, PersonalGoalTierGroup>();
		for (int i = 0; i < personalGoals.Count; i++)
		{
			GenericPersonalGoal personalGoal = personalGoals[i];
			string tierIdentifier = GetTierIdentifier(personalGoal);
			if (!dictionary.TryGetValue(tierIdentifier, out var value))
			{
				value = new PersonalGoalTierGroup();
				list.Add(value);
				dictionary.Add(tierIdentifier, value);
			}
			value.Add(personalGoal);
		}
		return list;
	}

	private static string GetTierIdentifier(GenericPersonalGoal personalGoal)
	{
		if (!string.IsNullOrEmpty(personalGoal.tierIdentifier))
		{
			return personalGoal.tierIdentifier;
		}
		if (string.IsNullOrEmpty(personalGoal.identifier))
		{
			return personalGoal.name;
		}
		return personalGoal.identifier;
	}

	private void ToggleSorting(SortCategory sortCategory)
	{
		if (_sortCategory != sortCategory)
		{
			_sortCategory = sortCategory;
			_sortDirection = SortDirection.Descending;
		}
		else if (_sortDirection == SortDirection.Descending)
		{
			_sortDirection = SortDirection.Ascending;
		}
		else
		{
			_sortCategory = SortCategory.None;
			_sortDirection = SortDirection.None;
		}
		RefreshSortArrows();
		ApplySorting();
	}

	private void ApplySorting()
	{
		ResetGoalEntryOrder();
		if (_sortCategory != SortCategory.None)
		{
			SortGoalEntries();
		}
		ApplyGoalEntryOrder();
	}

	private void ResetGoalEntryOrder()
	{
		for (int i = 0; i < _defaultGoalTierGroups.Count; i++)
		{
			_goalTierGroups[i] = _defaultGoalTierGroups[i];
		}
		for (int j = 0; j < _defaultGoalEntries.Count; j++)
		{
			_goalEntries[j] = _defaultGoalEntries[j];
		}
	}

	private void SortGoalEntries()
	{
		for (int i = 1; i < _goalTierGroups.Count; i++)
		{
			PersonalGoalTierGroup personalGoalTierGroup = _goalTierGroups[i];
			Transform value = _goalEntries[i];
			int num = i - 1;
			while (num >= 0 && ShouldSortBefore(personalGoalTierGroup, _goalTierGroups[num]))
			{
				_goalTierGroups[num + 1] = _goalTierGroups[num];
				_goalEntries[num + 1] = _goalEntries[num];
				num--;
			}
			_goalTierGroups[num + 1] = personalGoalTierGroup;
			_goalEntries[num + 1] = value;
		}
	}

	private bool ShouldSortBefore(PersonalGoalTierGroup goalTierGroup, PersonalGoalTierGroup otherGoalTierGroup)
	{
		int sortTierIndex = GetSortTierIndex(goalTierGroup);
		int sortTierIndex2 = GetSortTierIndex(otherGoalTierGroup);
		if (_sortDirection == SortDirection.Descending)
		{
			return sortTierIndex > sortTierIndex2;
		}
		return sortTierIndex < sortTierIndex2;
	}

	private int GetSortTierIndex(PersonalGoalTierGroup goalTierGroup)
	{
		if (_sortCategory != SortCategory.CurrentGoalTier)
		{
			return goalTierGroup.HighestCompletedSteamTierIndex;
		}
		return goalTierGroup.CurrentTierIndex;
	}

	private void RefreshSortArrows()
	{
		RefreshSortArrow(currentGoalTierSortArrow, SortCategory.CurrentGoalTier);
		RefreshSortArrow(steamAchievementTierSortArrow, SortCategory.SteamAchievementTier);
	}

	private void RefreshSortArrow(GameObject arrow, SortCategory sortCategory)
	{
		if (!(arrow == null))
		{
			bool flag = _sortCategory == sortCategory;
			arrow.SetActive(flag);
			if (flag)
			{
				arrow.transform.localEulerAngles = ((_sortDirection == SortDirection.Descending) ? DescendingSortArrowRotation : AscendingSortArrowRotation);
			}
		}
	}

	private void ApplyGoalEntryOrder()
	{
		int num = personalGoalTemplate.GetSiblingIndex() + 1;
		for (int i = 0; i < _goalEntries.Count; i++)
		{
			_goalEntries[i].SetSiblingIndex(num + i);
		}
	}

	private void SetUpPersonalGoal(PersonalGoalTierGroup goalTierGroup)
	{
		Transform transform = Object.Instantiate(personalGoalTemplate, personalGoalTemplate.parent);
		transform.GetComponent<PersonalGoalEntry>().Setup(goalTierGroup);
		transform.gameObject.SetActive(value: true);
		_goalEntries.Add(transform);
		_defaultGoalEntries.Add(transform);
	}

	[ConsoleMethod("personalgoals.reset", "Remove all completed personal goals", new string[] { })]
	public static void Command_Reset()
	{
		SaveGameManager.Current.completedPersonalGoals = new List<string>();
	}

	[ConsoleMethod("personalgoals.completeall", "Complete all personal goals", new string[] { })]
	public static void Command_CompleteAll()
	{
		SaveGameManager.Current.completedPersonalGoals = new List<string>();
		foreach (GenericPersonalGoal personalGoal in InstanceBehavior<GameManager>.Instance.personalGoals)
		{
			SaveGameManager.Current.completedPersonalGoals.Add(personalGoal.identifier);
		}
	}
}
