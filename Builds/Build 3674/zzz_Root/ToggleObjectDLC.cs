using Extensions;
using UnityEngine;

public class ToggleObjectDLC : MonoBehaviour
{
	public bool showWhenOwned;

	public SteamAPI.DLC dlc;

	private void OnEnable()
	{
		if (showWhenOwned)
		{
			base.gameObject.SetActive(dlc.DlcIsOwned());
		}
		else
		{
			base.gameObject.SetActive(!dlc.DlcIsOwned());
		}
	}
}
