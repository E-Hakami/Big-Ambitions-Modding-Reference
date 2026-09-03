using System;
using Entities;
using Extensions;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using NaughtyAttributes;
using TMPro;
using UI.Components;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan;

public class RealEstateSettings : MonoBehaviour
{
	[BoxGroup("Rent management")]
	[SerializeField]
	private GameObject rentManagementGameObject;

	[BoxGroup("Rent management")]
	[SerializeField]
	private TextLocalizationComponent currentRent;

	[BoxGroup("Rent management")]
	[SerializeField]
	private TextLocalizationComponent avgMarketPrice;

	[BoxGroup("Rent management")]
	[SerializeField]
	private Slider newRentSlider;

	[BoxGroup("Rent management")]
	[SerializeField]
	private Button applyRentButton;

	[BoxGroup("Rent management")]
	[SerializeField]
	private TMP_Text newRentValue;

	[BoxGroup("Rent management")]
	[SerializeField]
	private TextLocalizationComponent timeLeftUntilNewRent;

	[BoxGroup("Rent management")]
	[SerializeField]
	private TMP_Text occupancy;

	[BoxGroup("Rent management")]
	[SerializeField]
	private TextLocalizationComponent dailyRentLabel;

	[BoxGroup("Rent management")]
	[SerializeField]
	private TMP_Text dailyRentIncome;

	[BoxGroup("Self-occupied unit")]
	[SerializeField]
	private GameObject selfOccupiedUnitGameObject;

	[BoxGroup("Self-occupied unit")]
	[SerializeField]
	private TextLocalizationComponent selfOccupiedUnitDescription;

	[BoxGroup("Self-occupied unit")]
	[SerializeField]
	private TextLocalizationComponent selfOccupiedUnitState;

	[BoxGroup("Self-occupied unit")]
	[SerializeField]
	private Button selfOccupiedTakeoverUnitButton;

	[BoxGroup("Set for sale")]
	[SerializeField]
	private GameObject setForSaleGameObject;

	[BoxGroup("Set for sale")]
	[SerializeField]
	private RectTransform setForSaleNormalPosition;

	[BoxGroup("Set for sale")]
	[SerializeField]
	private RectTransform setForSaleHamptonsPosition;

	[BoxGroup("Set for sale")]
	[SerializeField]
	private TextLocalizationComponent priceAtPurchaseLabel;

	[BoxGroup("Set for sale")]
	[SerializeField]
	private TMP_Text priceAtPurchaseValue;

	[BoxGroup("Set for sale")]
	[SerializeField]
	private TMP_Text estimatedMarketPriceValue;

	[BoxGroup("Set for sale")]
	[SerializeField]
	private TMP_InputField salesPriceInput;

	[BoxGroup("Set for sale")]
	[SerializeField]
	private TMP_Text potentialGainValue;

	[BoxGroup("Set for sale")]
	[SerializeField]
	private TextLocalizationComponent cancelSaleLabel;

	[BoxGroup("Set for sale")]
	[SerializeField]
	private GameObject setForSaleButtonPanel;

	[BoxGroup("Set for sale")]
	[SerializeField]
	private GameObject setForSaleInputPanel;

	[BoxGroup("Set for sale")]
	[SerializeField]
	private GameObject cancelSetForSalePanel;

	private BizMan _bizMan;

	private RealEstate _realEstate;

	private float _marketRentPricePerSqm;

	private float _minRent;

	private float _maxRent;

	private float _rentVariation;

	private void Awake()
	{
		_bizMan = GetComponentInParent<BizMan>();
		newRentSlider.onValueChanged.AddListener(ChangeNewRentValue);
		selfOccupiedTakeoverUnitButton.onClick.AddListener(delegate
		{
			if (BuildingManager.IsInsideBuilding && InstanceBehavior<BuildingManager>.Instance.building.Address == _realEstate.address)
			{
				Notifications.ShowError("notification_cant_takeover_inside");
			}
			else
			{
				_bizMan.business.showTakeoverView = true;
				_bizMan.Open(_realEstate.address, "Presentation");
			}
		});
	}

	private void OnEnable()
	{
		_realEstate = SaveGameManager.Current.realEstate.Find((RealEstate x) => x.address == _bizMan.business.buildingRegistration.Address);
		if (_realEstate == null)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		bool flag = _realEstate.Building.IsHamptonsHouse();
		if (flag)
		{
			rentManagementGameObject.SetActive(value: false);
			selfOccupiedUnitGameObject.SetActive(value: false);
		}
		else
		{
			SetRentManagementData();
			SetSelfOccupiedUnitData();
		}
		SetForSaleData(flag);
		KeyboardInputHelper.Configure(salesPriceInput, SetForSale);
	}

	private void SetRentManagementData()
	{
		rentManagementGameObject.SetActive(value: true);
		_marketRentPricePerSqm = _realEstate.Building.GetBuildingDailyMarketRentPerSqm();
		_minRent = (float)Mathf.RoundToInt(_marketRentPricePerSqm * 0.8f * 100f) / 100f;
		_maxRent = (float)Mathf.RoundToInt(_marketRentPricePerSqm * 1.2f * 100f) / 100f;
		_rentVariation = _maxRent - _minRent;
		string areaUnit = UnitHelper.GetAreaUnit();
		float val = (UnitHelper.useImperial ? (_realEstate.pricePerSqm / 10.76391f) : _realEstate.pricePerSqm);
		float val2 = (UnitHelper.useImperial ? (_marketRentPricePerSqm / 10.76391f) : _marketRentPricePerSqm);
		currentRent.Arguments = new
		{
			rent = val.ToCurrencyFormat(),
			unit = areaUnit
		};
		avgMarketPrice.Arguments = new
		{
			price = val2.ToCurrencyFormat(),
			unit = areaUnit
		};
		float num = ((_realEstate.pendingPricePerSqm != 0f) ? _realEstate.pendingPricePerSqm : _realEstate.pricePerSqm);
		num = (UnitHelper.useImperial ? (num / 10.76391f) : num);
		newRentValue.text = num.ToCurrencyFormat();
		float num2 = (_maxRent - ((_realEstate.pendingPricePerSqm != 0f) ? _realEstate.pendingPricePerSqm : _realEstate.pricePerSqm)) / _rentVariation;
		newRentSlider.value = 1f - num2;
		if (_realEstate.daysUntilUpdatingPricePerSqm > 0)
		{
			timeLeftUntilNewRent.SetData(LanguageChangeEventDataHolder.Create("bizman_real_estate_time_left_for_new_rent", new
			{
				days = _realEstate.daysUntilUpdatingPricePerSqm
			}));
		}
		else
		{
			timeLeftUntilNewRent.SetData(LanguageChangeEventDataHolder.Create("bizman_real_estate_rent_applied"));
		}
		applyRentButton.interactable = false;
		string arg = ((_realEstate.OccupancyPercentage < 33) ? "#D4000B" : ((_realEstate.OccupancyPercentage < 50) ? "#CD9A49" : ((_realEstate.OccupancyPercentage < 75) ? "#DEE065" : "#0CA200")));
		occupancy.text = $"<color={arg}>{_realEstate.OccupancyPercentage}%</color>";
		dailyRentLabel.Arguments = new
		{
			occupancy = _realEstate.OccupancyPercentage,
			totalSqm = _realEstate.totalSqm.ToFormattedArea()
		};
		dailyRentIncome.text = _realEstate.DailyIncome.ToShortCurrencyFormat();
	}

	private void SetSelfOccupiedUnitData()
	{
		selfOccupiedUnitGameObject.SetActive(value: true);
		selfOccupiedUnitDescription.Arguments = new
		{
			buildingType = _bizMan.business.building.BuildingType
		};
		selfOccupiedUnitState.Key = (_bizMan.business.buildingRegistration.RentedByPlayer ? "bizman_real_estate_unit_owned_by_player" : "bizman_real_estate_unit_generating_rent");
		selfOccupiedTakeoverUnitButton.interactable = !_bizMan.business.buildingRegistration.RentedByPlayer;
	}

	private void SetForSaleData(bool isHamptonsHouse)
	{
		setForSaleGameObject.SetActive(value: true);
		setForSaleGameObject.GetRectTransform().position = (isHamptonsHouse ? setForSaleHamptonsPosition.position : setForSaleNormalPosition.position);
		double purchasePrice = _realEstate.purchasePrice;
		priceAtPurchaseLabel.Arguments = new
		{
			day = _realEstate.purchaseDay
		};
		priceAtPurchaseValue.text = purchasePrice.ToShortCurrencyFormat();
		double num = _bizMan.business.building.GetMarketValue();
		estimatedMarketPriceValue.text = num.ToShortCurrencyFormat();
		double num2 = num - purchasePrice;
		int num3 = ((purchasePrice != 0.0) ? ((int)Math.Round(num2 / purchasePrice * 100.0)) : 0);
		string arg = ((num3 > 0) ? "#0CA200" : ((num3 < 0) ? "#D4000B" : "#515A60"));
		potentialGainValue.text = $"{num2.ToShortCurrencyFormat()} <color={arg}>({num3}%)</color>";
		BuildingForSale buildingForSale = SaveGameManager.Current.buildingsForSale.Find((BuildingForSale x) => x.address == _realEstate.address);
		if (buildingForSale != null)
		{
			salesPriceInput.text = $"{buildingForSale.buildingPrice}";
			setForSaleButtonPanel.SetActive(value: false);
			setForSaleInputPanel.SetActive(value: false);
			cancelSaleLabel.Arguments = new
			{
				price = buildingForSale.buildingPrice.ToShortCurrencyFormat()
			};
			cancelSetForSalePanel.SetActive(value: true);
		}
		else
		{
			salesPriceInput.text = "";
			setForSaleButtonPanel.SetActive(value: true);
			setForSaleInputPanel.SetActive(value: true);
			cancelSetForSalePanel.SetActive(value: false);
		}
	}

	private void ChangeNewRentValue(float sliderValue)
	{
		float num = _minRent + (float)Mathf.RoundToInt(sliderValue * _rentVariation * 100f) / 100f;
		float val = (UnitHelper.useImperial ? (num / 10.76391f) : num);
		newRentValue.text = val.ToCurrencyFormat();
		applyRentButton.interactable = num != _realEstate.pendingPricePerSqm;
		if (applyRentButton.interactable)
		{
			timeLeftUntilNewRent.SetData(LanguageChangeEventDataHolder.Create("bizman_real_estate_rent_not_applied"));
		}
		else if (_realEstate.daysUntilUpdatingPricePerSqm > 0)
		{
			timeLeftUntilNewRent.SetData(LanguageChangeEventDataHolder.Create("bizman_real_estate_time_left_for_new_rent", new
			{
				days = _realEstate.daysUntilUpdatingPricePerSqm
			}));
		}
		else
		{
			timeLeftUntilNewRent.SetData(LanguageChangeEventDataHolder.Create("bizman_real_estate_rent_applied"));
		}
	}

	public void ApplyNewRent()
	{
		_realEstate.pendingPricePerSqm = _minRent + (float)Mathf.RoundToInt(newRentSlider.value * _rentVariation * 100f) / 100f;
		_realEstate.daysUntilUpdatingPricePerSqm = 7;
		timeLeftUntilNewRent.SetData(LanguageChangeEventDataHolder.Create("bizman_real_estate_time_left_for_new_rent", new
		{
			days = _realEstate.daysUntilUpdatingPricePerSqm
		}));
		applyRentButton.interactable = false;
	}

	public void SetForSale()
	{
		if (!float.TryParse(salesPriceInput.text, out var result))
		{
			Notifications.ShowError("bizman_real_estate_no_sales_price_notification", "NoSalesPrice");
		}
		else if (_realEstate.Building.IsHamptonsHouse())
		{
			HandleHamptonsHouseSale(result);
		}
		else
		{
			ConfirmSetForSale(result);
		}
	}

	private void HandleHamptonsHouseSale(float salesPrice)
	{
		if (BuildingManager.IsInsideBuilding && InstanceBehavior<BuildingManager>.Instance.building.Address == _realEstate.address)
		{
			Notifications.ShowError("bizman_real_estate_notification_cant_set_for_sale_when_inside");
			return;
		}
		LanguageChangeEventDataHolder bodyData = "bizman_real_estate_hud_confirm_set_for_sale".Localize();
		HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData, delegate
		{
			if (ItemHelper.AreThereSpecialGiftsInAddress(_realEstate.address))
			{
				LanguageChangeEventDataHolder bodyData2 = LanguageChangeEventDataHolder.Create("bizman_presentation_hud_confirm_special_gift_inside");
				HudConfirm.Show(default(LanguageChangeEventDataHolder), bodyData2, delegate
				{
					ConfirmSetForSale(salesPrice);
				});
			}
			else
			{
				ConfirmSetForSale(salesPrice);
			}
		});
	}

	private void ConfirmSetForSale(float salesPrice)
	{
		BuildingForSale item = new BuildingForSale
		{
			address = _realEstate.address,
			buildingPrice = salesPrice,
			squareMeters = _realEstate.totalSqm,
			acceptOfferRate = 1f
		};
		_bizMan.business.buildingRegistration.lastDayOnSale = SaveGameManager.Current.Day;
		SaveGameManager.Current.buildingsForSale.Add(item);
		setForSaleButtonPanel.SetActive(value: false);
		setForSaleInputPanel.SetActive(value: false);
		cancelSaleLabel.Arguments = new
		{
			price = salesPrice.ToShortCurrencyFormat()
		};
		cancelSetForSalePanel.SetActive(value: true);
		if (_realEstate.Building.IsHamptonsHouse())
		{
			BuildingManager.RefreshHamptonsHouseBlockerCollider(_realEstate.address);
		}
	}

	public void CancelForSale()
	{
		SaveGameManager.Current.buildingsForSale.RemoveAll((BuildingForSale x) => x.address == _realEstate.address);
		setForSaleButtonPanel.SetActive(value: true);
		setForSaleInputPanel.SetActive(value: true);
		cancelSetForSalePanel.SetActive(value: false);
		if (_realEstate.Building.IsHamptonsHouse())
		{
			BuildingManager.RefreshHamptonsHouseBlockerCollider(_realEstate.address);
		}
	}
}
