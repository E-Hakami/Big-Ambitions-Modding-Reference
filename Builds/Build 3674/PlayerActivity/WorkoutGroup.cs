using UnityEngine;

namespace PlayerActivity;

[CreateAssetMenu(fileName = "WorkoutGroup", menuName = "BigAmbitions/WorkoutGroup", order = 0)]
public class WorkoutGroup : ScriptableObject
{
	public WorkoutGroupType workoutGroupType;

	public WorkoutType[] workoutTypes;
}
