using System.Collections.Generic;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Player/HasJob")]
public class HasJob : QuestRequirement
{
	public override bool CheckIfCompleted()
	{
		List<JobInstance> jobInstances = SaveGameManager.Current.JobInstances;
		if (jobInstances != null)
		{
			foreach (JobInstance item in jobInstances)
			{
				if (item.hired)
				{
					return true;
				}
			}
		}
		return SaveGameManager.Current.currentPlayerMission != null;
	}
}
