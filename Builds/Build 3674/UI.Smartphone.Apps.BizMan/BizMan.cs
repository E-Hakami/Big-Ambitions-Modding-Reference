using UnityEngine;

namespace UI.Smartphone.Apps.BizMan;

public class BizMan : MonoBehaviour
{
	public const string PresentationTab = "Presentation";

	public const string DriversTab = "Drivers";

	public const string InventoryTab = "Inventory";

	public const string SettingsTab = "Settings";

	public const string FactoryTab = "Factory";

	public const string ScheduleTab = "Schedule";

	public const string PricingManagersTab = "PricingManagers";

	public const string LogisticsManagersTab = "LogisticsManagers";

	public const string PurchasingAgentsTab = "PurchasingAgents";

	public const string HrManagersTab = "HRManagers";

	public const string HeadhuntersTab = "Headhunters";

	public const string InsightTab = "Insight";

	public const string InventoryPricingTab = "InventoryPricing";

	public const string DeliveriesTab = "Deliveries";

	public const string MarketingTab = "Marketing";

	public const string RealEstateTab = "RealEstate";

	public BizManBusiness business;

	public BizManList list;

	private void OnEnable()
	{
		business.gameObject.SetActive(value: false);
		list.gameObject.SetActive(value: true);
	}

	private void OnDisable()
	{
		business.gameObject.SetActive(value: false);
		list.gameObject.SetActive(value: false);
	}

	public static bool CanOpenFromShortcut()
	{
		return InstanceBehavior<UIs>.Instance.smartphoneUI.gameObject.activeInHierarchy;
	}

	public void Open(Address newAddress = null, string tab = null)
	{
		if (!base.gameObject.activeSelf || !base.gameObject.activeInHierarchy)
		{
			InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.BizMan);
		}
		if (newAddress == null)
		{
			business.gameObject.SetActive(value: false);
			list.gameObject.SetActive(value: true);
			return;
		}
		business.SetAddress(newAddress);
		business.SetInitialTab(tab);
		list.gameObject.SetActive(value: false);
		business.Open();
	}
}
