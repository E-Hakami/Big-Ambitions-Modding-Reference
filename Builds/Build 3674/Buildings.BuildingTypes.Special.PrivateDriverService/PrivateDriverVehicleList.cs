using Extensions;
using UnityEngine;

namespace Buildings.BuildingTypes.Special.PrivateDriverService;

public class PrivateDriverVehicleList : MonoBehaviour
{
	[SerializeField]
	private PrivateDriverVehicleListEntry entryTemplate;

	[SerializeField]
	private CanvasGroup canvasGroup;

	private void Start()
	{
		entryTemplate.transform.ResetTemplate();
		foreach (VehicleInstance privateDriverVehicleInstance in SaveGameManager.Current.privateDriverVehicleInstances)
		{
			PrivateDriverVehicleListEntry privateDriverVehicleListEntry = Object.Instantiate(entryTemplate, entryTemplate.transform.parent);
			privateDriverVehicleListEntry.gameObject.SetActive(value: true);
			privateDriverVehicleListEntry.SetupEntry(privateDriverVehicleInstance);
		}
	}

	public void Disable()
	{
		canvasGroup.interactable = false;
	}
}
