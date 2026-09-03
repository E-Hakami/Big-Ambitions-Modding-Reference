using System.Linq;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Player/HasQuitJob")]
public class HasQuitJob : QuestRequirement
{
	public override bool CheckIfCompleted()
	{
		return SaveGameManager.Current.JobInstances.All((JobInstance x) => !x.hired);
	}
}
