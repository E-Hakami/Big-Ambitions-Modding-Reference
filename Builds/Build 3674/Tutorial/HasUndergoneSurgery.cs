using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/Requirements/Persona/HasUndergoneSurgery")]
public class HasUndergoneSurgery : QuestRequirement
{
	public int minTimes = 1;

	public override bool CheckIfCompleted()
	{
		return SaveGameManager.Current.numberOfDoctorOperations >= minTimes;
	}
}
