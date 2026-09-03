using Controllers;
using UnityEngine;

namespace Tutorial;

[CreateAssetMenu(menuName = "BigAmbitions/Quest/TutorialPointer/HideCondition/IsPlayingVideoGame")]
public class TutorialPointerHideConditionIfIsPlayingVideoGame : TutorialPointerHideCondition
{
	protected override bool ConditionMetInternal()
	{
		return VideoGameSetup.IsAnyVideoGamePlaying();
	}
}
