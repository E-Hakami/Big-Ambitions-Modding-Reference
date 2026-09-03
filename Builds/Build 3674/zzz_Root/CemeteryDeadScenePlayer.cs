using System.Collections;
using BigAmbitions.DayNightCycle;
using Helpers;
using UI;
using UnityEngine;

public class CemeteryDeadScenePlayer : MonoBehaviour
{
	[SerializeField]
	private CemeteryFuneralSetup setupObjectPrefab;

	[SerializeField]
	private Transform setupPivot;

	public IEnumerator PlayDeadScene()
	{
		if (setupObjectPrefab.CanPlay())
		{
			yield return UiFader.Fade();
			if (BuildingManager.IsInsideBuilding)
			{
				yield return InstanceBehavior<BuildingManager>.Instance.ExitFromBuildingCoroutine(0, playFadeAnimation: false);
			}
			Timestamp timestamp = TimeHelper.Now();
			if (timestamp.Hour >= 12)
			{
				timestamp.Day++;
			}
			timestamp.Hour = 12;
			timestamp.Minute = 0f;
			InstanceBehavior<UIs>.Instance.timeMachine.StartTimeMachine(timestamp, disableCancel: true, "funeral_timemachine_info", showBlur: false);
			yield return new WaitUntil(() => !InstanceBehavior<UIs>.Instance.timeMachine.isRunning);
			InstanceBehavior<UIs>.Instance.HideUI();
			InstanceBehavior<UIs>.Instance.gameSpeed.Set(new GameSpeed(paused: false, TimeSpeed.Normal));
			CemeteryFuneralSetup setupObject = Object.Instantiate(setupObjectPrefab, setupPivot.position, setupPivot.rotation, setupPivot);
			if (!setupObject.TryPlay(CameraHelper.GetCinemachineBrain()))
			{
				Object.Destroy(setupObject.gameObject);
				yield return UiFader.UnFade();
				yield break;
			}
			InstanceBehavior<GameManager>.Instance.playerController.Hide();
			yield return UiFader.UnFade(5f);
			float seconds = setupObject.Duration - 0.1f - 5f;
			yield return new WaitForSeconds(seconds);
			setupObject.Pause();
			InstanceBehavior<UIs>.Instance.funeralUI.gameObject.SetActive(value: true);
		}
	}
}
