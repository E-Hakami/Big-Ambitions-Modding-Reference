using Extensions;
using Helpers;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.ItemPanel;

public class VehicleInfoPanel : MonoBehaviour
{
	[Header("Steering Assist")]
	[SerializeField]
	private GameObject steeringAssist;

	[SerializeField]
	private Image steeringAssistIcon;

	[SerializeField]
	private Color steeringAssistActiveColor;

	[SerializeField]
	private Color steeringAssistInactiveColor;

	[Header("Parking Zone")]
	[SerializeField]
	private GameObject parkingZone;

	[SerializeField]
	private TextLocalizationComponent parkingZoneValue;

	[Header("Cargo")]
	[SerializeField]
	private GameObject cargo;

	[SerializeField]
	private TMP_Text cargoValue;

	[Header("Fuel")]
	[SerializeField]
	private GameObject fuel;

	[SerializeField]
	private TMP_Text fuelValue;

	[SerializeField]
	private Image fuelIcon;

	[Header("Condition")]
	[SerializeField]
	private GameObject condition;

	[SerializeField]
	private TMP_Text conditionValue;

	[SerializeField]
	private Image conditionIcon;

	[HideInInspector]
	public bool isCommonVehicle;

	[HideInInspector]
	public ParkingState currentParkingState;

	[HideInInspector]
	public string currentParkingNeighbourhood;

	private void OnEnable()
	{
		PlayerPrefs.Changed += OnPlayerPrefChanged;
		UpdateSteeringAssist();
	}

	private void OnDisable()
	{
		PlayerPrefs.Changed -= OnPlayerPrefChanged;
	}

	private void OnPlayerPrefChanged(PlayerPref playerPref)
	{
		if (playerPref == PlayerPref.SteeringAssist)
		{
			UpdateSteeringAssist();
		}
	}

	public void Initialize(VehicleController vehicle)
	{
		InstanceBehavior<GameManager>.Instance.selectedVehicle = vehicle;
		isCommonVehicle = vehicle.vehicleType.maxFuel != 0f;
		steeringAssist.SetActive(vehicle is CarController);
		parkingZone.SetActive(isCommonVehicle);
		fuel.SetActive(!SaveGameManager.Current.gameVariables.disableVehicleFuel && isCommonVehicle);
		condition.SetActive(!SaveGameManager.Current.gameVariables.disableVehicleDamage && isCommonVehicle);
		base.gameObject.SetActive(value: true);
	}

	public void UpdateInfo()
	{
		UpdateCargo();
		if (isCommonVehicle)
		{
			UpdateFuel();
			UpdateCondition();
		}
	}

	public void SetParkingZone(ParkingState state, string neighbourhood = null)
	{
		if (currentParkingState == state && currentParkingNeighbourhood == neighbourhood)
		{
			return;
		}
		currentParkingState = state;
		currentParkingNeighbourhood = neighbourhood;
		Color32 color = InstanceBehavior<GlobalReferences>.Instance.colors.black;
		switch (state)
		{
		case ParkingState.Illegal:
			parkingZoneValue.Key = "itempanelui_parkingzone_illegal";
			color = InstanceBehavior<GlobalReferences>.Instance.colors.red;
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.sleepButton.gameObject.SetActive(value: false);
			break;
		case ParkingState.Legal:
		{
			float num = NeighborhoodHelper.GetData(neighbourhood).parkingPrice;
			if (InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleInstance.streetName == "ba:street_parking")
			{
				num = 25f;
			}
			if (num == 0f)
			{
				parkingZoneValue.Key = "itempanelui_parkingzone_legal";
			}
			else
			{
				parkingZoneValue.Key = "itempanelui_parkingzone_legal_payed";
				parkingZoneValue.Arguments = new
				{
					price = num.ToShortCurrencyFormat()
				};
				color = InstanceBehavior<GlobalReferences>.Instance.colors.darkBlue;
			}
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.sleepButton.gameObject.SetActive(value: true);
			break;
		}
		case ParkingState.NotAvailable:
			parkingZoneValue.Key = "itempanelui_parkingzone_notavailable";
			InstanceBehavior<UIs>.Instance.playerHUD.itemPanelUI.sleepButton.gameObject.SetActive(value: false);
			break;
		}
		parkingZoneValue.TextContainer.color = color;
	}

	public void UpdateCargo()
	{
		int count = InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleInstance.cargoInstances.Count;
		int maxCargoCapacity = InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleType.maxCargoCapacity;
		cargoValue.text = $"{count}/{maxCargoCapacity}";
	}

	private void UpdateFuel()
	{
		int num = Mathf.CeilToInt(InstanceBehavior<GameManager>.Instance.selectedVehicle.GetCurrentFuel() / InstanceBehavior<GameManager>.Instance.selectedVehicle.vehicleType.maxFuel * 100f);
		Color32 color = ((num <= 30) ? InstanceBehavior<GlobalReferences>.Instance.colors.red : ((num > 50) ? InstanceBehavior<GlobalReferences>.Instance.colors.black : InstanceBehavior<GlobalReferences>.Instance.colors.orange));
		Color32 color2 = color;
		fuelValue.text = $"{num}%";
		fuelValue.color = color2;
		fuelIcon.color = color2;
	}

	private void UpdateCondition()
	{
		int num = Mathf.CeilToInt((1f - InstanceBehavior<GameManager>.Instance.selectedVehicle.GetCurrentCondition()) * 100f);
		Color32 color = ((num <= 30) ? InstanceBehavior<GlobalReferences>.Instance.colors.red : ((num > 50) ? InstanceBehavior<GlobalReferences>.Instance.colors.black : InstanceBehavior<GlobalReferences>.Instance.colors.orange));
		Color32 color2 = color;
		conditionValue.text = $"{num}%";
		conditionValue.color = color2;
		conditionIcon.color = color2;
	}

	public void ToggleSteeringAssist()
	{
		PlayerPrefSettings.SteeringAssist = !PlayerPrefSettings.SteeringAssist;
	}

	private void UpdateSteeringAssist()
	{
		steeringAssistIcon.color = (PlayerPrefSettings.SteeringAssist ? steeringAssistActiveColor : steeringAssistInactiveColor);
	}

	public bool CanSleep()
	{
		if (!isCommonVehicle)
		{
			return false;
		}
		return currentParkingState == ParkingState.Legal;
	}
}
