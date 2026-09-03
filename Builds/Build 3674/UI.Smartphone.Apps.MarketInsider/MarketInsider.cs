using Extensions;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.MarketInsider;

public class MarketInsider : MonoBehaviour
{
	public TextLocalizationComponent neighborhoodLabel;

	public MarketDemandScrollerController marketDemandScrollerController;

	public MarketEventScrollerController marketEventScrollerController;

	public MarketInsiderNeighborhoodData neighborhoodData;

	public RealEstateScrollerController realEstateScrollerController;

	public Transform tabTemplate;

	[SerializeField]
	private string initialTab = "ba:neighborhood_garmentdistrict";

	[SerializeField]
	private Button marketDemandsButton;

	[SerializeField]
	private Button realEstateButton;

	[SerializeField]
	private Transform marketDemandsPanel;

	[SerializeField]
	private Transform realEstatePanel;

	[SerializeField]
	private SplitterIndicator splitterIndicator;

	[SerializeField]
	private TogglePanel marketDemandFilterPanel;

	[SerializeField]
	private TogglePanel realEstateFilterPanel;

	private string _currentTab = "MarketDemands";

	private Color32 _defaultColor;

	private void Awake()
	{
		_defaultColor = tabTemplate.parent.GetComponentInChildren<TextMeshProUGUI>().color;
		LoadTabs();
	}

	private void OnEnable()
	{
		if (SaveGameManager.Current != null)
		{
			SetTab(GetInitialTab());
			GameEvent.Invoke("ba:gameevent_marketinsideropen");
		}
	}

	private void OnDisable()
	{
		marketDemandFilterPanel.Close();
		realEstateFilterPanel.Close();
		marketDemandScrollerController.UnloadDemands();
		marketDemandScrollerController.ClearFilters();
		realEstateScrollerController.ClearFilters();
	}

	private void LoadTabs()
	{
		tabTemplate.ResetTemplate();
		foreach (string neighborhood in NeighborhoodHelper.Neighborhoods)
		{
			if (!string.IsNullOrEmpty(neighborhood) && !(neighborhood == "ba:neighborhood_global"))
			{
				Transform obj = Object.Instantiate(tabTemplate, tabTemplate.parent);
				obj.name = neighborhood;
				obj.GetComponent<TextLocalizationComponent>().SetData(LanguageChangeEventDataHolder.Create(neighborhood));
				obj.GetComponent<Button>().onClick.AddListener(delegate
				{
					SetTab(neighborhood);
				});
				obj.gameObject.SetActive(value: true);
			}
		}
	}

	private void SetTab(string neighborhood)
	{
		Transform transform = null;
		foreach (Transform item in tabTemplate.parent)
		{
			bool flag = item.name == neighborhood;
			if (flag)
			{
				transform = item;
			}
			item.GetComponent<TextMeshProUGUI>().color = (flag ? InstanceBehavior<GlobalReferences>.Instance.colors.white : _defaultColor);
		}
		if (!(transform == null))
		{
			RectTransform component = transform.GetComponent<RectTransform>();
			splitterIndicator.Set(component);
			component.GetComponent<TextMeshProUGUI>().color = InstanceBehavior<GlobalReferences>.Instance.colors.white;
			neighborhoodLabel.SetData(neighborhood.Localize());
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				realEstateScrollerController.LoadRealEstate(neighborhood);
				marketDemandScrollerController.LoadDemands(neighborhood);
				marketEventScrollerController.LoadEvents(neighborhood);
				neighborhoodData.ShowNeighborhoodData(neighborhood);
				ChangeTab(_currentTab);
			});
		}
	}

	private string GetInitialTab()
	{
		string currentNeighborhood = NeighborhoodHelper.CurrentNeighborhood;
		if (!string.IsNullOrEmpty(currentNeighborhood))
		{
			return currentNeighborhood;
		}
		return initialTab;
	}

	public void ChangeTab(string newTab)
	{
		_currentTab = newTab;
		marketDemandsPanel.gameObject.SetActive(newTab == "MarketDemands");
		realEstatePanel.gameObject.SetActive(newTab == "RealEstate");
		marketDemandsButton.interactable = newTab != "MarketDemands";
		realEstateButton.interactable = newTab != "RealEstate";
		marketDemandsButton.transform.GetLabelByName("Label").color = ((newTab == "MarketDemands") ? InstanceBehavior<GlobalReferences>.Instance.colors.black : InstanceBehavior<GlobalReferences>.Instance.colors.darkGrey);
		realEstateButton.transform.GetLabelByName("Label").color = ((newTab == "RealEstate") ? InstanceBehavior<GlobalReferences>.Instance.colors.black : InstanceBehavior<GlobalReferences>.Instance.colors.darkGrey);
	}
}
