using System.Linq;
using Helpers;
using UnityEngine;

namespace Tutorial.Tutorial2GetSomeFood;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Quest Actions/Complete Objectives")]
public class QuestActionCompleteObjectives : QuestAction
{
	[SerializeField]
	private string[] questObjectiveIds;

	public override void Do()
	{
		string[] array = questObjectiveIds;
		foreach (string questObjectiveId in array)
		{
			if (!SaveGameManager.Current.CompletedQuestEntries.Contains(questObjectiveId))
			{
				SaveGameManager.Current.CompletedQuestEntries.Add(questObjectiveId);
				TutorialHelper.currentQuest.entries.First((QuestEntry x) => x.Id == questObjectiveId).Complete();
				if (TutorialHelper.currentQuestEntry.Id == questObjectiveId)
				{
					TutorialHelper.LoadNextQuestEntry();
				}
			}
		}
	}
}
