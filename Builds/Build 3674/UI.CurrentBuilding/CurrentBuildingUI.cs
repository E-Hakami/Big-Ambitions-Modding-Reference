using System;
using System.Collections.Generic;
using BigAmbitions.InteriorDesigner;
using BigAmbitions.Tags;
using Buildings.Indoors;
using Extensions;
using Helpers;
using IngameDebugConsole;
using Localizor;
using Localizor.LanguageChangeEvent;
using Parking.UndergroundParking;
using Streets;
using TMPro;
using Tooltip;
using UI.InteriorDesigner;
using UnityEngine;
using UnityEngine.UI;

namespace UI.CurrentBuilding;

public class CurrentBuildingUI : MonoBehaviour
{
	[SerializeField]
	private GameObject panel;

	[SerializeField]
	private TextLocalizationComponent openStateLabel;

	[SerializeField]
	private GameObject openLabelContainer;

	[SerializeField]
	private TextMeshProUGUI headlineLabel;

	[SerializeField]
	private GameObject splitter;

	[SerializeField]
	private GameObject bizManButton;

	[SerializeField]
	private GameObject temporaryClosedContainer;

	[SerializeField]
	private Toggle temporarilyClosedToggle;

	[Header("Grouped actions")]
	[SerializeField]
	private GameObject[] ownerActions;

	[SerializeField]
	private GameObject[] splitterRequiredObjects;

	[SerializeField]
	private GameObject[] buildingEntries;

	[Header("Wall button stuff")]
	public Image wallsButtonImage;

	public Sprite wallsVisibleSprite;

	public Sprite wallsHiddenSprite;

	public Sprite wallsPartlyHiddenSprite;

	[Header("Parking info")]
	[SerializeField]
	private GameObject parkingInfoPanel;

	[SerializeField]
	private TextLocalizationComponent parkingPriceLabel;

	[SerializeField]
	private TextLocalizationComponent parkedVehiclesLabel;

	[SerializeField]
	private LocalizedListTooltip parkedVehiclesTooltip;

	private void Start()
	{
		GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, new Action(UpdateData));
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, (Action<Address>)delegate
		{
			UpdateData();
		});
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, (Action<Address>)delegate
		{
			UpdateData();
			Toggle(on: true);
		});
		GlobalEvents.onExitBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onExitBuilding, (Action<Address>)delegate
		{
			Toggle(on: false);
		});
		UndergroundParkingManager.onEnter = (Action)Delegate.Combine(UndergroundParkingManager.onEnter, (Action)delegate
		{
			UpdateData();
			Toggle(on: true);
		});
		UndergroundParkingManager.onExit = (Action)Delegate.Combine(UndergroundParkingManager.onExit, new Action(OnExit));
		GlobalEvents.onEnterVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onEnterVehicle, (Action<VehicleController>)delegate
		{
			if (UndergroundParkingManager.IsInsideParking)
			{
				UpdateData();
			}
		});
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onExitVehicle, (Action<VehicleController>)delegate
		{
			if (UndergroundParkingManager.IsInsideParking)
			{
				UpdateData();
			}
		});
		temporarilyClosedToggle.onValueChanged.AddListener(delegate(bool closed)
		{
			if (InstanceBehavior<BuildingManager>.Instance.buildingRegistration != null)
			{
				InstanceBehavior<BuildingManager>.Instance.buildingRegistration.TemporarilyClose(closed);
			}
		});
		parkingPriceLabel.Arguments = new
		{
			price = 25f.ToShortCurrencyFormat()
		};
		WallsVisibilityHelper.onWallsVisibilityChanged = (Action<WallsVisibility>)Delegate.Combine(WallsVisibilityHelper.onWallsVisibilityChanged, new Action<WallsVisibility>(UpdateWallsVisibilityIcon));
		UpdateWallsVisibilityIcon(WallsVisibilityHelper.currentWallsVisibility);
	}

	private void OnExit()
	{
		Toggle(on: true);
	}

	public void Toggle()
	{
		Toggle(!panel.activeInHierarchy);
	}

	public void Toggle(bool on)
	{
		if (on && !ShouldShowPanel())
		{
			on = false;
		}
		panel.SetActive(on);
	}

	public void UpdateData()
	{
		if (!ShouldShowPanel())
		{
			Toggle(on: false);
			return;
		}
		if (UndergroundParkingManager.IsInsideParking)
		{
			UpdateParkingData();
		}
		else
		{
			UpdateBuildingData();
		}
		UpdateSplitter();
	}

	private void UpdateSplitter()
	{
		bool active = false;
		GameObject[] array = splitterRequiredObjects;
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].activeSelf)
			{
				active = true;
				break;
			}
		}
		splitter.gameObject.SetActive(active);
	}

	private void UpdateBuildingData()
	{
		headlineLabel.text = (string.IsNullOrEmpty(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.BusinessName) ? InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Address.ToFormattedString() : InstanceBehavior<BuildingManager>.Instance.buildingRegistration.BusinessName);
		parkingInfoPanel.SetActive(value: false);
		ToggleBuildingEntries(newState: true);
		ToggleOwnerActions(InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness);
		parkingPriceLabel.gameObject.SetActive(value: false);
		parkedVehiclesLabel.gameObject.SetActive(value: false);
		bizManButton.SetActive(value: true);
		if (InstanceBehavior<BuildingManager>.Instance.businessType == null || (!InstanceBehavior<BuildingManager>.Instance.businessType.HasTag(TagRef.Businesstag.generatesrevenue) && InstanceBehavior<BuildingManager>.Instance.businessType.businessTypeName != "ba:businesstype_factory"))
		{
			openStateLabel.Key = "";
			openStateLabel.TextContainer.text = "";
			temporaryClosedContainer.SetActive(value: false);
			if (CasinoBoatManager.IsOnCasinoBoat)
			{
				bizManButton.SetActive(value: false);
				int num = 5 - TimeHelper.CurrentHour;
				string key = ((num == 1) ? "current_building_ui_closing_in_hour" : "current_building_ui_closing_in_hours");
				openStateLabel.Key = key;
				openStateLabel.TextContainer.color = ((num <= 1) ? InstanceBehavior<GlobalReferences>.Instance.colors.orange : InstanceBehavior<GlobalReferences>.Instance.colors.lime);
				openStateLabel.Arguments = new
				{
					hours = num,
					time = 5.GetFormattedTime()
				};
			}
			UpdateOpenLabelActiveState();
		}
		else
		{
			temporarilyClosedToggle.SetIsOnWithoutNotify(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.temporarilyClosed);
			bool flag = BusinessHelper.IsBusinessOpen(InstanceBehavior<BuildingManager>.Instance.buildingRegistration);
			openStateLabel.Key = (flag ? "common_open" : "common_closed");
			openStateLabel.TextContainer.color = (flag ? InstanceBehavior<GlobalReferences>.Instance.colors.lime : InstanceBehavior<GlobalReferences>.Instance.colors.red);
			UpdateOpenLabelActiveState();
		}
	}

	private void UpdateParkingData()
	{
		Address address = UndergroundParkingManager.currentParkingEntrance.parentCbc.buildingRegistration.Address;
		headlineLabel.text = "current_building_ui_parking".Localize(new
		{
			address = address.ToFormattedString()
		}).ToString();
		ToggleOwnerActions(newState: false);
		ToggleBuildingEntries(newState: false);
		temporaryClosedContainer.SetActive(value: false);
		parkingInfoPanel.SetActive(value: true);
		parkingPriceLabel.gameObject.SetActive(value: true);
		List<string> parkedVehicles = GetParkedVehicles();
		if (parkedVehicles.Count > 0)
		{
			parkedVehiclesLabel.Arguments = new
			{
				numberOfParkedVehicles = parkedVehicles.Count
			};
			parkedVehiclesTooltip.list = parkedVehicles;
			parkedVehiclesLabel.gameObject.SetActive(value: true);
		}
		else
		{
			parkedVehiclesLabel.gameObject.SetActive(value: false);
		}
	}

	private void ToggleBuildingEntries(bool newState)
	{
		GameObject[] array = buildingEntries;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(newState);
		}
	}

	private void ToggleOwnerActions(bool newState)
	{
		GameObject[] array = ownerActions;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].SetActive(newState);
		}
	}

	private void UpdateOpenLabelActiveState()
	{
		openLabelContainer.SetActive(!string.IsNullOrEmpty(openStateLabel.Key));
	}

	private static List<string> GetParkedVehicles()
	{
		List<string> list = new List<string>();
		VehicleInstance currentVehicle = VehicleHelper.GetCurrentVehicle();
		int parkingNumber = UndergroundParkingManager.currentParkingEntrance.parkingNumber;
		foreach (VehicleInstance vehicleInstance in SaveGameManager.Current.VehicleInstances)
		{
			if (vehicleInstance != currentVehicle && !(vehicleInstance.streetName != "ba:street_parking") && vehicleInstance.streetNumber == parkingNumber)
			{
				list.Add(vehicleInstance.vehicleTypeName);
			}
		}
		return list;
	}

	public void OpenBuildingInfo()
	{
		InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Address);
	}

	public void ToggleInteriorDesigner()
	{
		if (InteriorDesignerUI.IsOpen)
		{
			InstanceBehavior<UIs>.Instance.interiorDesignerUI.Hide();
			return;
		}
		InstanceBehavior<UIs>.Instance.interiorDesignerUI.DisableTool(ToolName.Furniture);
		InstanceBehavior<UIs>.Instance.interiorDesignerUI.DisableTool(ToolName.Duplicate);
		InstanceBehavior<UIs>.Instance.interiorDesignerUI.Show();
	}

	public void ToggleWalls()
	{
		WallsVisibilityHelper.ToggleWalls();
	}

	private void UpdateWallsVisibilityIcon(WallsVisibility wallsVisibility)
	{
		Image image = wallsButtonImage;
		image.sprite = wallsVisibility switch
		{
			WallsVisibility.AllVisible => wallsVisibleSprite, 
			WallsVisibility.AllHidden => wallsHiddenSprite, 
			_ => wallsPartlyHiddenSprite, 
		};
	}

	private bool ShouldShowPanel()
	{
		if (GameManager.IsAnyMiniGameActive())
		{
			return false;
		}
		if (InstanceBehavior<BuildingManager>.Instance?.buildingRegistration == null)
		{
			return UndergroundParkingManager.IsInsideParking;
		}
		return true;
	}

	private void OnDestroy()
	{
		WallsVisibilityHelper.onWallsVisibilityChanged = (Action<WallsVisibility>)Delegate.Remove(WallsVisibilityHelper.onWallsVisibilityChanged, new Action<WallsVisibility>(UpdateWallsVisibilityIcon));
		UndergroundParkingManager.onExit = (Action)Delegate.Remove(UndergroundParkingManager.onExit, new Action(OnExit));
	}

	[ConsoleMethod("ToggleWallsVisibility", "Changes the wall visibility type to visible, hidden or partly hidden", new string[] { })]
	public static void Command_ToggleWallsVisibility()
	{
		InstanceBehavior<UIs>.Instance.interiorDesignerUI.ToggleWalls();
	}
}
