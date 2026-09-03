using System.Linq;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Courses/HasCompletedCourse")]
public class HasCompletedCourse : QuestRequirement
{
	public DiplomaName diplomaName;

	public override bool CheckIfCompleted()
	{
		return SaveGameManager.Current.PlayerDiplomas.Any((Diploma x) => x.name == diplomaName && x.completed);
	}
}
