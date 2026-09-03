using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Player/HasEnergy")]
public class HasEnergy : QuestRequirement
{
	[SerializeField]
	private float minEnergy;

	public override bool CheckIfCompleted()
	{
		return SaveGameManager.Current.Energy >= minEnergy;
	}
}
