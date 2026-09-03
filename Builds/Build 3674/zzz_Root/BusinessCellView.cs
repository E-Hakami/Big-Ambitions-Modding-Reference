using System.Collections.Generic;
using BaTable;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using Streets;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class BusinessCellView : BaTableCellView<BusinessCellView.BusinessModel>
{
	public sealed class BusinessModel
	{
		public string Id;

		public string BusinessName;

		public string BusinessType;

		public string BuildingType;

		public Address Address;

		public float AvgDailyIncome;

		public int Alerts;

		public int Satisfaction;

		public (LanguageChangeEventDataHolder text, Color color) OpenStatus;

		public bool IsRealEstate;

		public readonly List<string> statuses = new List<string>();

		public int Status;

		public string Name
		{
			get
			{
				if (!string.IsNullOrEmpty(BusinessName))
				{
					return BusinessName;
				}
				return Address.ToFormattedString();
			}
		}

		public BusinessModel(BuildingRegistration instance)
		{
			BusinessName = instance.BusinessName;
			BusinessType = instance.businessTypeName;
			BuildingType = instance.GetBuildingType();
			Address = instance.Address;
			Alerts = instance.Alerts;
			IsRealEstate = false;
			AvgDailyIncome = instance.GetAvgDailyIncome(7);
			Satisfaction = instance.satisfaction.overall;
			OpenStatus = instance.GetOpenStatus();
			Status = (string.IsNullOrEmpty(instance.BusinessName) ? (instance.BuildingCached.BuildingType.Length - 100) : (BusinessHelper.IsBusinessOpen(instance) ? (-1) : (-2)));
			statuses.Add(instance.HasEstablishedBusiness ? "common_businesses" : "ba:businesstype_empty");
		}

		public BusinessModel(BuildingRegistration instance, bool isRealEstate)
		{
			Address = instance.Address;
			BuildingType = instance.GetBuildingType();
			BusinessType = "ba:businesstype_empty";
			IsRealEstate = isRealEstate;
			AvgDailyIncome = instance.RealEstate.DailyIncome;
			OpenStatus = instance.GetOccupancyStatus();
			Status = instance.RealEstate.OccupancyPercentage;
			statuses.Add("common_real_estate");
		}
	}

	[SerializeField]
	private GameObject hoverOutline;

	public Button destinationButton;

	public TextMeshProUGUI businessName;

	public TextLocalizationComponent businessType;

	public TextMeshProUGUI avgDailyIncome;

	public TextLocalizationComponent alerts;

	public TextMeshProUGUI satisfaction;

	public TextLocalizationComponent status;

	public override void SetData(BusinessModel data)
	{
		destinationButton.onClick.RemoveAllListeners();
		destinationButton.onClick.AddListener(delegate
		{
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.SetDestination(data.Address);
		});
		status.SetData(data.OpenStatus.text);
		status.TextContainer.color = data.OpenStatus.color;
		if (!string.IsNullOrEmpty(data.BusinessName))
		{
			businessName.text = data.BusinessName;
			businessType.SetData(data.BusinessType.Localize());
			avgDailyIncome.text = data.AvgDailyIncome.ToShortCurrencyFormat();
			avgDailyIncome.color = ((data.AvgDailyIncome < 0f) ? InstanceBehavior<GlobalReferences>.Instance.colors.lightRed : InstanceBehavior<GlobalReferences>.Instance.colors.white);
			if (data.Alerts == 0)
			{
				alerts.SetValue("0");
				alerts.TextContainer.color = Color.white;
			}
			else
			{
				alerts.SetData(LanguageChangeEventDataHolder.Create("bizman_number_of_alerts", new
				{
					amount = data.Alerts
				}));
				alerts.TextContainer.color = InstanceBehavior<GlobalReferences>.Instance.colors.lightRed;
			}
			satisfaction.text = $"{data.Satisfaction}%";
			satisfaction.color = ((data.Satisfaction < 50) ? InstanceBehavior<GlobalReferences>.Instance.colors.lightRed : InstanceBehavior<GlobalReferences>.Instance.colors.white);
			return;
		}
		businessName.text = data.Address.ToFormattedString();
		alerts.SetValue("-");
		alerts.TextContainer.color = Color.white;
		satisfaction.text = "-";
		satisfaction.color = InstanceBehavior<GlobalReferences>.Instance.colors.white;
		if (data.IsRealEstate)
		{
			businessType.SetData(LanguageChangeEventDataHolder.Create("bizman_list_real_estate_type", new
			{
				buildingType = data.BuildingType
			}));
			avgDailyIncome.text = data.AvgDailyIncome.ToShortCurrencyFormat();
			avgDailyIncome.color = ((data.AvgDailyIncome < 0f) ? InstanceBehavior<GlobalReferences>.Instance.colors.red : InstanceBehavior<GlobalReferences>.Instance.colors.white);
		}
		else
		{
			avgDailyIncome.text = "-";
			avgDailyIncome.color = InstanceBehavior<GlobalReferences>.Instance.colors.white;
			businessType.SetData("bizman_empty_building".Localize(new
			{
				buildingType = data.BuildingType
			}));
		}
	}

	public override void ResetVisuals()
	{
		if ((bool)hoverOutline)
		{
			hoverOutline.SetActive(value: false);
		}
	}

	public override void OnPointerEnter(PointerEventData eventData)
	{
		if ((bool)hoverOutline)
		{
			hoverOutline.SetActive(value: true);
		}
	}

	public override void OnPointerExit(PointerEventData eventData)
	{
		if ((bool)hoverOutline)
		{
			hoverOutline.SetActive(value: false);
		}
	}

	public override void VisualizeSelected(bool selected)
	{
	}
}
