using System.Collections.Generic;
using System.Linq;
using Entities;
using Localizor;
using UI.Elements;
using UI.Notification;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.PurchasingAgent;

public class PurchasingAgentProductsMassActionsUI : MonoBehaviour
{
	public static readonly List<ImportProduct> massActionSelectedProducts = new List<ImportProduct>();

	public Toggle massActionAllToggle;

	[SerializeField]
	private UI.Elements.Dropdown massActionDropdown;

	private CanvasGroup _massActionDropdownCanvasGroup;

	private List<BuildingRegistration> _warehouses;

	private PurchasingAgentPlanUI PlanUI => InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.purchasingAgentsPlanList.purchasingAgentPlanUISettings;

	private PurchasingAgentProductsScrollerController ScrollerController => PlanUI.productsScrollerController;

	private static bool IsContractActive => InstanceBehavior<UIs>.Instance.fullMenu.bizMan.business.purchasingAgentsPlanList.purchasingAgentPlanUISettings.IsContractActive;

	public void Start()
	{
		_massActionDropdownCanvasGroup = massActionDropdown.GetComponent<CanvasGroup>();
		massActionAllToggle.onValueChanged.AddListener(MassActionToggleAll);
		massActionDropdown.SetPlaceholder("bizman_purchasingagents_mass_action_dropdown_placeholder");
		massActionDropdown.onOptionSelected.RemoveAllListeners();
		massActionDropdown.onOptionSelected.AddListener(DoMassAction);
		UpdateDropdown(null);
	}

	private void MassActionToggleAll(bool toggled)
	{
		massActionSelectedProducts.Clear();
		if (toggled)
		{
			massActionSelectedProducts.AddRange(PlanUI.CurrentIpProducts);
		}
		UpdateDropdownInteractable();
		ScrollerController.scroller.RefreshActiveCellViews();
	}

	public void ToggleSelectedProduct(ImportProduct importProduct, bool toggled)
	{
		if (toggled)
		{
			if (!massActionSelectedProducts.Contains(importProduct))
			{
				massActionSelectedProducts.Add(importProduct);
			}
		}
		else
		{
			massActionSelectedProducts.Remove(importProduct);
		}
		bool isOnWithoutNotify = massActionSelectedProducts.Count == PlanUI.CurrentIpProducts.Count;
		massActionAllToggle.SetIsOnWithoutNotify(isOnWithoutNotify);
		UpdateDropdownInteractable();
	}

	public void ToggleRange(int firstSelectedIndex, int lastSelectedIndex, bool newState)
	{
		int num = ((firstSelectedIndex <= lastSelectedIndex) ? firstSelectedIndex : lastSelectedIndex);
		int num2 = ((firstSelectedIndex > lastSelectedIndex) ? firstSelectedIndex : lastSelectedIndex);
		for (int i = num; i <= num2; i++)
		{
			ImportProduct productRef = ScrollerController.data[i].productRef;
			ToggleSelectedProduct(productRef, newState);
		}
		ScrollerController.scroller.RefreshActiveCellViews();
	}

	public void UpdateDropdown(List<BuildingRegistration> warehouses)
	{
		_warehouses = warehouses ?? new List<BuildingRegistration>();
		_warehouses.Insert(0, null);
		massActionDropdown.SetOptions(_warehouses.Select((BuildingRegistration x) => (x != null) ? x.GetDisplayName() : "common_unassigned".GetLocalization()).ToList(), localize: false);
		UpdateDropdownInteractable();
	}

	private void DoMassAction(int index)
	{
		if (massActionSelectedProducts.Count == 0)
		{
			Notifications.ShowError("myemployees_mass_action_no_employees_selected");
		}
		else
		{
			MassDesignateWarehouse(_warehouses[index]);
		}
		massActionDropdown.ResetSelectedOption();
	}

	private void MassDesignateWarehouse(BuildingRegistration warehouse)
	{
		if (IsContractActive)
		{
			return;
		}
		foreach (ImportProduct product in massActionSelectedProducts)
		{
			product.assignedWarehouse = warehouse?.Address;
			PurchasingAgentProductModel purchasingAgentProductModel = ScrollerController.data.First((PurchasingAgentProductModel x) => x.productRef == product);
			purchasingAgentProductModel.UpdateWarehouse();
			purchasingAgentProductModel.updateContractLabels();
		}
		massActionDropdown.HideOptions();
		ScrollerController.scroller.RefreshActiveCellViews();
	}

	public void UpdateDropdownInteractable()
	{
		bool flag = massActionSelectedProducts.Count > 0 && !IsContractActive;
		massActionDropdown.SetInteractable(flag);
		_massActionDropdownCanvasGroup.alpha = (flag ? 1f : 0.5f);
	}

	private void OnDisable()
	{
		massActionDropdown.HideOptions();
	}
}
