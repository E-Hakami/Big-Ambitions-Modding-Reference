using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BigAmbitions.GameAnalytics;
using Blueprints;
using Entities;
using IngameDebugConsole;
using Streets;
using Tutorial;
using Tutorial.SideQuests;
using UI;
using UI.Guiders;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Helpers;

public static class TutorialHelper
{
	public const float NewGameEnergy = 50f;

	public const float MaxLoanAmountBeforeCompletingObjectiveGetFirstLoan = 15500f;

	public const string ObjectiveToStartAskingForRent = "tutorial_quest_get_some_sleep_objective_4";

	public const string ObjectiveWorkAtCashRegister = "tutorial_quest_open_the_store_objective_2";

	public const string ObjectiveToStartHavingHealthInsuranceDemands = "tutorial_quest_logistics_network_objective_5";

	public const string ObjectiveWhereCleaningTasksStartAppearing = "tutorial_quest_cleaning_objective_3";

	public const string ObjectiveRentFirstRetail = "tutorial_quest_establish_first_business_objective_2";

	public const string ObjectiveRentSecondRetail = "tutorial_quest_another_business_objective_2";

	public const string ObjectiveRentSmallOffice = "tutorial_quest_first_hq_objective_2";

	public const string ObjectiveGetFirstLoan = "tutorial_quest_establish_first_business_objective_1";

	private const string AddressableLabel = "Quests";

	private const string UncleFredContactId = "uncle_fred";

	private const string UncleFredContactDescription = "friends_and_family";

	private const ContactCategoryName UncleFredContactCategory = ContactCategoryName.General;

	public static readonly Address InitialApartment = new Address("ba:street_thirdstreet", 45);

	public static readonly Address ElGatoAddress = new Address("ba:street_thirdstreet", 23);

	public const string TutorialHelpNeighborhood = "ba:neighborhood_garmentdistrict";

	public const string TutorialBuildingTypeRetail = "ba:buildingtype_retail";

	public static readonly BuildingSizeInfo TutorialSizeInfo = new BuildingSizeInfo("ba:buildingsize_a", 3);

	public static Action onQuestLoaded;

	public static Action onQuestEntryCompleted;

	public static Quest[] allQuests;

	public static Quest currentQuest;

	public static QuestEntry currentQuestEntry;

	private static readonly HashSet<string> GameEventsToCheck = new HashSet<string>();

	public static bool IsTutorialEnabled()
	{
		return SaveGameManager.Current.gameVariables.tutorialEnabled;
	}

	public static bool HasCompletedObjective(string objectiveId)
	{
		if (IsTutorialEnabled())
		{
			return SaveGameManager.Current.CompletedQuestEntries.Contains(objectiveId);
		}
		return true;
	}

	public static string GetForcedHelpPageSlug()
	{
		if (SaveGameManager.Current == null || !IsTutorialEnabled() || allQuests == null)
		{
			return null;
		}
		Quest quest = currentQuest;
		if (quest == null)
		{
			return null;
		}
		QuestEntry[] entries = quest.entries;
		for (int i = 0; i < entries.Length; i++)
		{
			if (!HasCompletedObjective(entries[i].Id))
			{
				string forcedHelpPageSlug = entries[i].GetForcedHelpPageSlug();
				if (!string.IsNullOrEmpty(forcedHelpPageSlug))
				{
					return forcedHelpPageSlug;
				}
			}
		}
		return null;
	}

	public static Quest GetNextQuest()
	{
		return allQuests.FirstOrDefault((Quest x) => x.entries.Any((QuestEntry entry) => !HasCompletedObjective(entry.Id)));
	}

	public static bool IsTutorialFinished()
	{
		return allQuests.All((Quest x) => x.entries.All((QuestEntry qe) => SaveGameManager.Current.CompletedQuestEntries.Contains(qe.Id)));
	}

	public static void OnGameEventTriggered(string gameEvent)
	{
		GameEventsToCheck.Add(gameEvent);
	}

	public static void Init()
	{
		if (allQuests == null)
		{
			ReloadQuests();
		}
		if (IsTutorialEnabled() && !IsTutorialFinished())
		{
			EnableTutorial();
			return;
		}
		InstanceBehavior<UIs>.Instance.tutorialUI.enabled = false;
		InstanceBehavior<UIs>.Instance.sideQuestUI.enabled = false;
	}

	public static void LateUpdate()
	{
		if (InstanceBehavior<UIs>.Instance.timeMachine.isRunning || GameManager.isCitySceneBeingUnloaded)
		{
			return;
		}
		foreach (string item in GameEventsToCheck)
		{
			CheckForCompletedObjectives(item);
		}
		GameEventsToCheck.Clear();
	}

	public static void ReloadQuests()
	{
		allQuests = (from x in Addressables.LoadAssetsAsync<Quest>("Quests", null).WaitForCompletion()
			orderby x.order
			select x).ToArray();
	}

	public static void NewTutorial()
	{
		if (allQuests == null)
		{
			ReloadQuests();
		}
		Quest quest = allQuests[0];
		Contact contact = new Contact("uncle_fred", ContactCategoryName.General, "friends_and_family");
		contact.SendMessage(new TextMessage(quest.uncleFredMessageType, null, read: true));
		SaveGameManager.Current.Contacts.Add(contact);
		GameAnalytics.TrackStartObjective(quest.Id, SaveGameManager.Current.Day, TimeHelper.GetCurrentFormattedTimeForced());
	}

	public static void EnableTutorial()
	{
		SaveGameManager.Current.gameVariables.tutorialEnabled = true;
		currentQuest = GetNextQuest();
		currentQuestEntry = GetNextQuestEntry();
		CheckForCompletedObjectives(string.Empty);
		TutorialPointersManager.UpdateTutorialPointers(currentQuestEntry, DirectionGuiderType.MainQuest);
		SetQuestTarget(currentQuestEntry, DirectionGuiderType.MainQuest);
		InitQuestEntries();
		InstanceBehavior<UIs>.Instance.tutorialUI.Enable();
		GlobalEvents.onTimeMachineEnded = (Action)Delegate.Combine(GlobalEvents.onTimeMachineEnded, new Action(OnTimeMachineEnded));
		GameEvent.onGameEventTriggered = (Action<string>)Delegate.Combine(GameEvent.onGameEventTriggered, new Action<string>(OnGameEventTriggered));
		SideQuestHelper.EnableSideQuests();
	}

	private static void InitQuestEntries()
	{
		if (!(currentQuest == null))
		{
			QuestEntry[] entries = currentQuest.entries;
			for (int i = 0; i < entries.Length; i++)
			{
				entries[i].Init();
			}
		}
	}

	public static void DisableTutorial()
	{
		SaveGameManager.Current.gameVariables.tutorialEnabled = false;
		InstanceBehavior<UIs>.Instance.tutorialUI.Disable();
		currentQuest = null;
		currentQuestEntry = null;
		TutorialPointersManager.UpdateTutorialPointers(currentQuestEntry, DirectionGuiderType.MainQuest);
		GuidersManager.ResetGuider(DirectionGuiderType.MainQuest);
		GlobalEvents.onTimeMachineEnded = (Action)Delegate.Remove(GlobalEvents.onTimeMachineEnded, new Action(OnTimeMachineEnded));
		GameEvent.onGameEventTriggered = (Action<string>)Delegate.Remove(GameEvent.onGameEventTriggered, new Action<string>(OnGameEventTriggered));
		SideQuestHelper.DisableSideQuests();
	}

	public static QuestEntry GetNextQuestEntry()
	{
		if (currentQuest == null)
		{
			return null;
		}
		Quest nextQuest = currentQuest;
		if (currentQuest.entries.All((QuestEntry x) => HasCompletedObjective(x.Id)))
		{
			nextQuest = GetNextQuest();
		}
		if (nextQuest == null)
		{
			return null;
		}
		return nextQuest.entries.FirstOrDefault((QuestEntry entry) => !HasCompletedObjective(entry.Id));
	}

	public static void LoadNextQuestEntry()
	{
		QuestEntry[] entries = currentQuest.entries;
		foreach (QuestEntry questEntry in entries)
		{
			if (!HasCompletedObjective(questEntry.Id))
			{
				currentQuestEntry = questEntry;
				TutorialPointersManager.UpdateTutorialPointers(questEntry, DirectionGuiderType.MainQuest);
				SetQuestTarget(currentQuestEntry, DirectionGuiderType.MainQuest);
				currentQuestEntry.Init();
				GameAnalytics.TrackStartObjective(questEntry.Id, SaveGameManager.Current.Day, TimeHelper.GetCurrentFormattedTimeForced());
				CheckForCompletedObjectives(string.Empty);
				return;
			}
		}
		currentQuest = GetNextQuest();
		if (currentQuest == null)
		{
			currentQuestEntry = null;
			TutorialPointersManager.UpdateTutorialPointers(null, DirectionGuiderType.MainQuest);
			GuidersManager.ResetGuider(DirectionGuiderType.MainQuest);
			onQuestLoaded?.Invoke();
			return;
		}
		QuestAction[] onStartActions = currentQuest.onStartActions;
		for (int i = 0; i < onStartActions.Length; i++)
		{
			onStartActions[i].Do();
		}
		onQuestLoaded?.Invoke();
		InitQuestEntries();
		Contact.GetContact("uncle_fred", ContactCategoryName.General, "friends_and_family").SendMessage(new TextMessage(currentQuest.uncleFredMessageType, null, read: true));
		LoadNextQuestEntry();
	}

	public static void SetQuestTarget(QuestEntry questEntry, DirectionGuiderType guiderType)
	{
		if (questEntry == null)
		{
			return;
		}
		if (questEntry.target == null)
		{
			GuidersManager.ResetGuider(guiderType);
			return;
		}
		questEntry.target.guiderType = guiderType;
		questEntry.target.Init();
		Address address = questEntry.target.GetAddress();
		if (!address.IsUndefined())
		{
			GuidersManager.SetGuiderTarget(address, guiderType);
			return;
		}
		questEntry.target.guiderType = guiderType;
		questEntry.target.SetTarget();
	}

	private static void OnTimeMachineEnded()
	{
		CheckForCompletedObjectives(string.Empty);
	}

	private static void CheckForCompletedObjectives(string gameEvent)
	{
		if (currentQuest == null)
		{
			return;
		}
		QuestEntry[] entries = currentQuest.entries;
		foreach (QuestEntry questEntry in entries)
		{
			if (!HasCompletedObjective(questEntry.Id) && questEntry.CheckIfCompleted(gameEvent))
			{
				CompleteQuestEntry(questEntry);
			}
		}
	}

	private static void CompleteQuestEntry(QuestEntry questEntry)
	{
		questEntry.Complete();
		GameAnalytics.TrackCompleteObjective(questEntry.Id, SaveGameManager.Current.Day, TimeHelper.GetCurrentFormattedTimeForced());
		UiSoundHelper.Play(UiSound.ObjectiveCompleted);
		onQuestEntryCompleted?.Invoke();
		LoadNextQuestEntry();
	}

	[ConsoleMethod("TutorialReloadQuests", "Completes the current task", new string[] { })]
	public static void Command_ReloadQuests()
	{
		ReloadQuests();
		TutorialPointersManager.UpdateTutorialPointers(currentQuestEntry, DirectionGuiderType.MainQuest);
	}

	[ConsoleMethod("TutorialCompleteObjective", "Completes the current objective", new string[] { })]
	public static void Command_CompleteObjective()
	{
		if (IsTutorialEnabled())
		{
			QuestEntry nextQuestEntry = GetNextQuestEntry();
			if (nextQuestEntry != null)
			{
				CompleteQuestEntry(nextQuestEntry);
			}
		}
	}

	[ConsoleMethod("TutorialCompleteQuest", "Completes the current quest", new string[] { })]
	public static void Command_CompleteQuest()
	{
		if (!IsTutorialEnabled())
		{
			return;
		}
		QuestEntry[] entries = currentQuest.entries;
		foreach (QuestEntry questEntry in entries)
		{
			if (!HasCompletedObjective(questEntry.Id))
			{
				CompleteQuestEntry(questEntry);
			}
		}
	}

	[ConsoleMethod("TutorialJumpToQuest", "Completes all quests up until the given quest number", new string[] { })]
	public static void Command_JumpToQuest(int questObjectiveNumber)
	{
		if (!IsTutorialEnabled())
		{
			return;
		}
		Quest nextQuest = GetNextQuest();
		while (nextQuest != null && nextQuest.order < questObjectiveNumber)
		{
			QuestEntry[] entries = nextQuest.entries;
			foreach (QuestEntry questEntry in entries)
			{
				if (!HasCompletedObjective(questEntry.Id))
				{
					CompleteQuestEntry(questEntry);
				}
			}
			nextQuest = GetNextQuest();
		}
	}

	[ConsoleMethod("TutorialClearCompletedObjectives", "Empties the save game list of completed objectives", new string[] { })]
	public static void Command_ClearCompletedObjectives()
	{
		SaveGameManager.Current.CompletedQuestEntries.Clear();
		LoadNextQuestEntry();
	}

	[ConsoleMethod("TutorialGetQuestsList", "Shows a list of quests in the console", new string[] { })]
	public static void Command_GetQuestsList()
	{
		Debug.Log($"{allQuests.Length} quests loaded!");
		StringBuilder stringBuilder = new StringBuilder();
		Quest[] array = allQuests;
		foreach (Quest quest in array)
		{
			stringBuilder.AppendLine($"{quest.order}: {quest.name}");
			QuestEntry[] entries = quest.entries;
			foreach (QuestEntry questEntry in entries)
			{
				stringBuilder.AppendLine("- " + questEntry.Id);
			}
			Debug.Log(stringBuilder);
			stringBuilder.Clear();
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		allQuests = null;
		currentQuest = null;
		currentQuestEntry = null;
		GameEventsToCheck.Clear();
		onQuestLoaded = null;
		onQuestEntryCompleted = null;
	}
}
