using PlayerActivity;
using UnityEngine;

namespace Controllers;

public interface IWorkoutMachine
{
	WorkoutExercise GetWorkoutExercise();

	HandObjectData GetHandObjectData();

	void TogglePhysics(bool enable = true);

	Animator GetAnimator();

	void ShowOccupiedNotification();

	Transform GetCharacterPoint();

	Transform GetEndPoint();

	void PlayAudioClip(AudioClip audioClip, bool loop = false);

	void StopAudio();
}
