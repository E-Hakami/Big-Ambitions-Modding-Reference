using System;
using System.Linq;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using Streets;
using TMPro;
using UI.Guiders;
using UI.Load;
using UnityEngine;
using UnityEngine.UI;

namespace UI.InGameUI;

public class BuildingResume : MonoBehaviour
{
	public TextLocalizationComponent addressLabel;

	public TextLocalizationComponent rivalBuildingOwner;

	public TextMeshProUGUI businessNameLabel;

	public TextLocalizationComponent neighborhoodLabel;

	public TextLocalizationComponent rivalBusinessOwner;

	public TextLocalizationComponent businessTypeLabel;

	public TextLocalizationComponent openStateLabel;

	public TextMeshProUGUI trafficIndexLabel;

	public TextLocalizationComponent buildingAreaLabel;

	public TextMeshProUGUI customerCapacityLabel;

	public TextLocalizationComponent taxiTravelButtonText;

	public TextMeshProUGUI clickToEnterLabel;

	public TextMeshProUGUI clickToOptionsLabel;

	public Button bizManButton;

	public Button taxiTravelButton;

	public Button setDestinationButton;

	public Button changeSignButton;

	public RectTransform options;

	public RectTransform data;

	public float businessRentedByYouFontSize = 30f;

	[SerializeField]
	private Canvas canvas;

	[SerializeField]
	private RectTransform canvasRectTransform;

	[SerializeField]
	private RectTransform panelRectTransform;

	[SerializeField]
	private float backwardsOffsetFromEntrancePosition;

	[SerializeField]
	private float edgesOffset;

	private bool _optionsAreVisible;

	private Color _rivalBusinessOwnerOriginalColor;

	private float _rivalBusinessOwnerOriginalSize;

	public CityBuildingController CityBuildingController { get; private set; }

	private void Awake()
	{
		_rivalBusinessOwnerOriginalColor = rivalBusinessOwner.TextContainer.color;
		_rivalBusinessOwnerOriginalSize = rivalBusinessOwner.TextContainer.fontSize;
	}

	public void Start()
	{
		data.gameObject.SetActive(value: true);
		options.gameObject.SetActive(value: false);
		GlobalEvents.onEnterBuilding = (Action<Address>)Delegate.Combine(GlobalEvents.onEnterBuilding, (Action<Address>)delegate
		{
			Close();
		});
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate
		{
			Close();
		});
		GlobalEvents.onFullMenuToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onFullMenuToggle, (Action<bool>)delegate
		{
			Close();
		});
		GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, (Action<Address>)delegate
		{
			UpdateDetails();
		});
		GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, new Action(UpdateDetails));
		if (CityBuildingController?.buildingRegistration == null)
		{
			panelRectTransform.gameObject.SetActive(value: false);
		}
	}

	private void LateUpdate()
	{
		if (!GameManager.GetMainCamera() || !InstanceBehavior<CityManager>.Instance)
		{
			return;
		}
		if ((bool)CityBuildingController?.building)
		{
			Vector3 vector = GameManager.GetMainCamera().WorldToScreenPointProjected(CityBuildingController.entranceDoors[0].doorTransform.position - CityBuildingController.entranceDoors[0].doorTransform.forward * backwardsOffsetFromEntrancePosition);
			RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, vector, canvas.worldCamera, out var localPoint);
			Rect rect = panelRectTransform.rect;
			Rect rect2 = canvasRectTransform.rect;
			if (localPoint.x + rect2.width / 2f < rect.width / 2f + edgesOffset)
			{
				localPoint.x = (0f - rect2.width) / 2f + rect.width / 2f + edgesOffset;
			}
			else if (localPoint.x + rect2.width / 2f + rect.width / 2f > rect2.width - edgesOffset)
			{
				localPoint.x = rect2.width / 2f - rect.width / 2f - edgesOffset;
			}
			if (localPoint.y + rect2.height / 2f < edgesOffset)
			{
				localPoint.y = (0f - rect2.height) / 2f + edgesOffset;
			}
			else if (localPoint.y + rect2.height / 2f + rect.height > rect2.height - edgesOffset)
			{
				localPoint.y = rect2.height / 2f - rect.height - edgesOffset;
			}
			panelRectTransform.anchoredPosition = localPoint;
		}
		panelRectTransform.gameObject.SetActive(CityBuildingController?.building != null);
	}

	public void ShowOptions(CityBuildingController cityBuildingController)
	{
		if (CityBuildingController == cityBuildingController && _optionsAreVisible)
		{
			ToggleOptions(show: false);
			return;
		}
		SelectBuilding(cityBuildingController);
		ToggleOptions(show: true);
	}

	public void HoverBuilding(CityBuildingController cityBuildingController)
	{
		if (!_optionsAreVisible)
		{
			SelectBuilding(cityBuildingController);
		}
	}

	public void UnHoverBuilding(CityBuildingController cityBuildingController)
	{
		if (!_optionsAreVisible)
		{
			UnselectBuilding(cityBuildingController);
		}
	}

	public void SelectBuilding(CityBuildingController cityBuildingController)
	{
		if (!(CityBuildingController == cityBuildingController))
		{
			if (_optionsAreVisible)
			{
				ToggleOptions(show: false);
			}
			CityBuildingController = cityBuildingController;
			if (CityBuildingController.building != null)
			{
				UpdateDetails();
			}
		}
	}

	public void UnselectBuilding(CityBuildingController cityBuildingController)
	{
		if (!(cityBuildingController != CityBuildingController))
		{
			CityBuildingController = null;
		}
	}

	private void UpdateDetails()
	{
		if (LoadScene.isLoading || CityBuildingController?.building == null)
		{
			return;
		}
		addressLabel.SetValue(CityBuildingController.building.Address.ToFormattedString(), clearKey: true);
		neighborhoodLabel.Arguments = new
		{
			neighbourhood = CityBuildingController.building.Neighbourhood
		};
		BuildingRegistration buildingRegistration = CityBuildingController.buildingRegistration;
		businessNameLabel.text = ((buildingRegistration != null) ? buildingRegistration.BusinessName : "common_unknown".GetLocalization());
		string key = ((CityBuildingController.building.BuildingType == "ba:buildingtype_residential") ? "ba:buildingtype_residential" : ((buildingRegistration.businessTypeName == "ba:businesstype_empty") ? CityBuildingController.building.BuildingType : buildingRegistration.businessTypeName));
		businessTypeLabel.Key = key;
		trafficIndexLabel.text = CityBuildingController.building.trafficIndex.ToString();
		bool flag = CityBuildingController.building.GetCustomerCapacity >= 0;
		customerCapacityLabel.gameObject.SetActive(flag);
		if (flag)
		{
			customerCapacityLabel.text = CityBuildingController.building.GetCustomerCapacity.ToString();
		}
		rivalBusinessOwner.gameObject.SetActive(value: false);
		if (CityBuildingController.building.BuildingType != "ba:buildingtype_warehouse" && !string.IsNullOrEmpty(buildingRegistration.BusinessName))
		{
			if (buildingRegistration.RentedByPlayer)
			{
				rivalBusinessOwner.gameObject.SetActive(value: true);
				rivalBusinessOwner.SetData(new LanguageChangeEventDataHolder
				{
					Key = "buildingresume_rented_by_you"
				});
				rivalBusinessOwner.TextContainer.color = InstanceBehavior<GlobalReferences>.Instance.colors.yellow;
				rivalBusinessOwner.TextContainer.fontSize = businessRentedByYouFontSize;
			}
			else if (!string.IsNullOrEmpty(buildingRegistration.businessOwnerRivalId))
			{
				rivalBusinessOwner.gameObject.SetActive(value: true);
				rivalBusinessOwner.SetData(BusinessHelper.GetBusinessOwnerDescription(buildingRegistration));
				rivalBusinessOwner.TextContainer.color = _rivalBusinessOwnerOriginalColor;
				rivalBusinessOwner.TextContainer.fontSize = _rivalBusinessOwnerOriginalSize;
			}
		}
		if (ShouldShowOwner())
		{
			rivalBuildingOwner.SetData(BusinessHelper.GetBuildingOwnerDescription(buildingRegistration));
			rivalBuildingOwner.gameObject.SetActive(value: true);
		}
		else
		{
			rivalBuildingOwner.gameObject.SetActive(value: false);
		}
		BuildingHelper.SetBuildingAreaLocalization(buildingAreaLabel, CityBuildingController.building);
		changeSignButton.gameObject.SetActive(!CityMap.IsOpen && buildingRegistration.RentedByPlayer && CityBuildingController.building.BuildingType != "ba:buildingtype_residential" && CityBuildingController.HasHorizontalSigns);
		SetClickLabels();
		openStateLabel.gameObject.SetActive(value: true);
		string buildingType = CityBuildingController.building.BuildingType;
		if (buildingType == "ba:buildingtype_residential" || buildingType == "ba:buildingtype_warehouse")
		{
			if (buildingRegistration.RentedByPlayer)
			{
				string key2 = (buildingRegistration.BuildingCached.IsHamptonsHouse() ? "buildingresume_owned_by_you" : "buildingresume_rented_by_you");
				openStateLabel.Key = key2;
				openStateLabel.Prefix = "";
				openStateLabel.TextContainer.color = InstanceBehavior<GlobalReferences>.Instance.colors.yellow;
			}
			else
			{
				bool availableForRent = buildingRegistration.AvailableForRent;
				openStateLabel.Key = (availableForRent ? "common_available_for_rent" : "buildingresume_occupied");
				openStateLabel.Prefix = "";
				openStateLabel.TextContainer.color = (availableForRent ? InstanceBehavior<GlobalReferences>.Instance.colors.green : InstanceBehavior<GlobalReferences>.Instance.colors.red);
			}
		}
		else
		{
			bool flag2 = BusinessHelper.IsBusinessOpen(buildingRegistration);
			int currentHour = TimeHelper.CurrentHour;
			bool availableForRent2 = buildingRegistration.AvailableForRent;
			if (availableForRent2)
			{
				openStateLabel.Key = "common_available_for_rent";
				openStateLabel.Prefix = "";
			}
			else if (flag2)
			{
				try
				{
					OpeningHourSlot openingHourSlot = buildingRegistration.scheduleDays.Find((ScheduleDay x) => x.day == TimeHelper.GetDayOfWeek()).openingHourSlots.First((OpeningHourSlot x) => x.startingHour <= currentHour && x.endingHour > currentHour);
					int num = openingHourSlot.endingHour - currentHour;
					if (openingHourSlot.endingHour >= 23 && buildingRegistration.scheduleDays.Find((ScheduleDay x) => x.day == TimeHelper.GetNextDayOfWeek()).openingHourSlots.Min((OpeningHourSlot x) => x.startingHour) == 0)
					{
						num = -1;
					}
					object key3;
					switch (num)
					{
					case -1:
						openStateLabel.SetData("common_open".Localize());
						goto end_IL_047b;
					default:
						key3 = "buildingresume_open_closing_in_hours";
						break;
					case 1:
						key3 = "buildingresume_open_closing_in_hour";
						break;
					}
					LanguageChangeEventDataHolder languageChangeEventDataHolder = LanguageChangeEventDataHolder.Create((string)key3, new
					{
						hours = num,
						time = openingHourSlot.endingHour.GetFormattedTime()
					}, "common_open");
					openStateLabel.SetData(languageChangeEventDataHolder);
					end_IL_047b:;
				}
				catch (Exception)
				{
					openStateLabel.Key = "common_open";
					openStateLabel.Prefix = "";
				}
			}
			else if (buildingRegistration.temporarilyClosed)
			{
				openStateLabel.Key = "common_closed";
				openStateLabel.Prefix = "";
			}
			else
			{
				try
				{
					int num2 = 0;
					int num3 = -1;
					int num4 = buildingRegistration.scheduleDays.FindIndex((ScheduleDay x) => x.day == TimeHelper.GetDayOfWeek());
					ScheduleDay scheduleDay = buildingRegistration.scheduleDays[num4];
					OpeningHourSlot openingHourSlot2 = scheduleDay.openingHourSlots.OrderBy((OpeningHourSlot x) => x.startingHour).FirstOrDefault((OpeningHourSlot x) => x.startingHour > currentHour);
					if (scheduleDay.isOpen && openingHourSlot2 != null)
					{
						num3 = openingHourSlot2.startingHour;
						num2 = num3 - currentHour;
					}
					else
					{
						num2 = 24 - currentHour;
						int num5 = 0;
						while (num3 == -1 && num5 < 7)
						{
							num4++;
							num5++;
							if (num4 >= buildingRegistration.scheduleDays.Count)
							{
								num4 = 0;
							}
							scheduleDay = buildingRegistration.scheduleDays[num4];
							if (scheduleDay.isOpen)
							{
								num3 = scheduleDay.openingHourSlots.Min((OpeningHourSlot x) => x.startingHour);
								num2 += num3;
							}
							else
							{
								num2 += 24;
							}
						}
					}
					if (num3 == -1)
					{
						openStateLabel.Key = "common_closed";
						openStateLabel.Prefix = "";
					}
					else
					{
						LanguageChangeEventDataHolder languageChangeEventDataHolder2 = LanguageChangeEventDataHolder.Create((num2 == 1) ? "buildingresume_closed_opening_in_hour" : "buildingresume_closed_opening_in_hours", new
						{
							hours = num2,
							time = num3.GetFormattedTime()
						}, "common_closed");
						openStateLabel.SetData(languageChangeEventDataHolder2);
					}
				}
				catch (Exception)
				{
					openStateLabel.Key = "common_closed";
					openStateLabel.Prefix = "";
				}
			}
			openStateLabel.TextContainer.color = (flag2 ? InstanceBehavior<GlobalReferences>.Instance.colors.green : (availableForRent2 ? InstanceBehavior<GlobalReferences>.Instance.colors.blue : InstanceBehavior<GlobalReferences>.Instance.colors.red));
		}
		if (CityMap.IsOpen)
		{
			options.gameObject.SetActive(value: true);
			data.gameObject.SetActive(value: true);
		}
		SetButtons();
	}

	private bool ShouldShowOwner()
	{
		if (string.IsNullOrEmpty(CityBuildingController.buildingRegistration.buildingOwnerRivalId) && !CityBuildingController.buildingRegistration.BuildingCached.IsHamptonsAIVilla())
		{
			return CityBuildingController.buildingRegistration.BuildingOwnedByPlayer;
		}
		return true;
	}

	private void SetButtons()
	{
		bizManButton.gameObject.SetActive(CityBuildingController.buildingRegistration != null);
		setDestinationButton.gameObject.SetActive(CityMap.IsOpen && !InstanceBehavior<CityManager>.Instance.cityMap.IsTaxiMode);
		taxiTravelButton.gameObject.SetActive(InstanceBehavior<CityManager>.Instance.cityMap.IsTaxiMode);
	}

	public void ShowInfo()
	{
		InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(CityBuildingController.building.Address);
		ToggleOptions(show: false);
		Close();
	}

	public void SetDestination()
	{
		ToggleOptions(show: false);
		GuidersManager.SetGuiderTarget(CityBuildingController.building.Address, DirectionGuiderType.Destination);
		InstanceBehavior<CityManager>.Instance.cityMap.Toggle();
	}

	public void TaxiTravel()
	{
		InstanceBehavior<TaxiSystem>.Instance.TravelTo(CityBuildingController);
	}

	public void Close()
	{
		CityBuildingController?.Unselect();
		UnselectBuilding(CityBuildingController);
		if (_optionsAreVisible)
		{
			ToggleOptions(show: false);
		}
	}

	public void ToggleSignAppearance()
	{
		InstanceBehavior<UIs>.Instance.draggableWindows.signAppearance.SetBuilding(CityBuildingController.buildingRegistration);
		UnselectBuilding(CityBuildingController);
	}

	public void ToggleOptions(bool show)
	{
		_optionsAreVisible = show;
		options.gameObject.SetActive(_optionsAreVisible);
		data.gameObject.SetActive(!_optionsAreVisible);
		if ((bool)CityBuildingController)
		{
			SetClickLabels();
		}
	}

	private void SetClickLabels()
	{
		clickToEnterLabel.gameObject.SetActive(!CityMap.IsOpen && !VehicleHelper.IsInsideMotorVehicle() && !_optionsAreVisible && BuildingHelper.CanEnterBuilding(CityBuildingController.buildingRegistration.Address));
		clickToOptionsLabel.gameObject.SetActive(!CityMap.IsOpen && !_optionsAreVisible);
		if (InstanceBehavior<CityManager>.Instance.cityMap.IsTaxiMode)
		{
			float distance = Vector3.Distance(CityBuildingController.GetNavMeshTargetPosition(), InstanceBehavior<GameManager>.Instance.playerController.transform.position);
			float price = TaxiSystem.GetPrice(distance);
			taxiTravelButtonText.Key = "ba:buildingresume_taxi_travel";
			float travelDurationMinutes = TaxiSystem.GetTravelDurationMinutes(distance);
			string text = "bizman_factory_production_time_per_product_label".Localize(new
			{
				minutes = Mathf.CeilToInt(travelDurationMinutes)
			}).ToString();
			if (price > 0f)
			{
				text = text + ", " + price.ToShortCurrencyFormat();
			}
			taxiTravelButtonText.Arguments = new
			{
				info = text
			};
		}
	}
}
