using BigAmbitions.Characters;
using NaughtyAttributes;
using RoboRyanTron.SearchableEnum;
using UnityEngine;

namespace PlayerActivity;

[CreateAssetMenu(fileName = "WorkoutExercise", menuName = "BigAmbitions/WorkoutExercise", order = 0)]
public class WorkoutExercise : ScriptableObject
{
	public WorkoutType workoutType;

	[SearchableEnum]
	public PermanentAnimationType animation;

	public AudioClip permanentAudioClip;

	public AudioClip finishAudioClip;

	public AudioClip[] oneTimeAudioClips;

	public PlayerActivityBalanceConfig balanceConfig;

	public PlayerActivityBalanceConfig workoutPlanCompletionBalanceConfig;

	public float energyConsumptionPerMinute;

	public bool hasAnimationOnTheWorkoutMachine;

	[ShowIf("hasAnimationOnTheWorkoutMachine")]
	[AllowNesting]
	public string animationOnTheWorkoutMachineBoolName;

	[SearchableEnum]
	public AnimationType[] seriesAnimations;

	public int minMinutesBetweenSeriesAnimations;

	public int maxMinutesBetweenSeriesAnimations;

	public int amountPerSeries;

	public int minMinutesBreakBetweenSeries;

	public int maxMinutesBreakBetweenSeries;

	public AnimationType[] randomAnimations;

	public int minMinutesBetweenRandomAnimations;

	public int maxMinutesBetweenRandomAnimations;

	public int GetDefaultMinutes()
	{
		int workoutMinutes = SaveGameManager.Current.PlayerDefaults.workoutMinutes;
		return balanceConfig.GetDefaultMinutes(workoutMinutes);
	}

	public void SetDefaultMinutes(int minutes)
	{
		SaveGameManager.Current.PlayerDefaults.workoutMinutes = minutes;
	}
}
