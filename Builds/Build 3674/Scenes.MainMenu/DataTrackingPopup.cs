using UnityEngine;

namespace Scenes.MainMenu;

public class DataTrackingPopup : MonoBehaviour
{
	public void ChooseTracking(bool allowTracking)
	{
		PlayerPrefSettings.allowTracking = allowTracking;
		PlayerPrefSettings.ShowDataTrackingPopup = false;
		base.gameObject.SetActive(value: false);
		InstanceBehavior<MainMenuController>.Instance.NextMainMenuAction();
	}
}
