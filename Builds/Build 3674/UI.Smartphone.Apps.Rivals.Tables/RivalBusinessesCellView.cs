using BaTable;
using Extensions;
using Localizor;
using Localizor.LanguageChangeEvent;
using Streets;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.Rivals.Tables;

public class RivalBusinessesCellView : BaTableCellView<RivalBusinessesCellView.RivalBusinessModel>
{
	public class RivalBusinessModel
	{
		public string BusinessName;

		public string BusinessType;

		public float WeeklyIncome;

		public Address Address;

		public RivalBusinessModel(string businessName, string businessType, float weeklyIncome, Address address)
		{
			BusinessName = businessName;
			BusinessType = businessType;
			WeeklyIncome = weeklyIncome;
			Address = address;
		}
	}

	[SerializeField]
	private GameObject hoverOutline;

	[SerializeField]
	private Button cellButton;

	public TextMeshProUGUI businessName;

	public TextLocalizationComponent businessType;

	public TextMeshProUGUI weeklyIncome;

	public TextLocalizationComponent address;

	public Button showOnMapButton;

	public Button setDestinationButton;

	public override void SetData(RivalBusinessModel data)
	{
		businessName.SetText(data.BusinessName);
		businessType.SetData(data.BusinessType.Localize());
		weeklyIncome.SetText(data.WeeklyIncome.ToShortCurrencyFormat());
		weeklyIncome.color = ((data.WeeklyIncome < 0f) ? InstanceBehavior<GlobalReferences>.Instance.colors.red : InstanceBehavior<GlobalReferences>.Instance.colors.white);
		address.SetValue(data.Address.ToFormattedString(), clearKey: true);
		setDestinationButton.onClick.RemoveAllListeners();
		setDestinationButton.onClick.AddListener(delegate
		{
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.SetDestination(data.Address);
		});
		showOnMapButton.onClick.RemoveAllListeners();
		showOnMapButton.onClick.AddListener(delegate
		{
			InstanceBehavior<UIs>.Instance.fullMenu.CloseFullMenu();
			InstanceBehavior<CityManager>.Instance.cityMap.FocusOnBuilding(InstanceBehavior<CityManager>.Instance.FindCityBuildingController(data.Address));
		});
		cellButton.onClick.RemoveAllListeners();
		cellButton.onClick.AddListener(delegate
		{
			InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(data.Address);
		});
	}

	public override void VisualizeSelected(bool selected)
	{
	}
}
