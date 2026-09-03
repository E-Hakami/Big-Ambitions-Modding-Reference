using System;
using JimmysUnityUtilities;
using UI.Elements;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan;

public class BizManList : MonoBehaviour
{
	[SerializeField]
	private BusinessScrollerController businessScrollerController;

	[SerializeField]
	private PrivateResidenceScrollerController privateResidenceScrollerController;

	[SerializeField]
	private WarehouseList warehouseList;

	[SerializeField]
	private HeadquartersList headquartersList;

	[Header("Filters")]
	[SerializeField]
	private TogglePanel bizManFilterPanel;

	[SerializeField]
	private Badge filterBadge;

	private void Start()
	{
		businessScrollerController.onRowSelected.AddListener(OpenBusiness);
		privateResidenceScrollerController.onRowSelected.AddListener(OpenResidential);
	}

	private void OnEnable()
	{
		BusinessScrollerController obj = businessScrollerController;
		obj.onDataChanged = (Action)Delegate.Combine(obj.onDataChanged, new Action(UpdateFilterBadge));
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			businessScrollerController.LoadList();
			privateResidenceScrollerController.Load();
			warehouseList.Load();
			headquartersList.Load();
		});
	}

	private void OnDisable()
	{
		bizManFilterPanel.Close();
		businessScrollerController.ClearFilters();
		BusinessScrollerController obj = businessScrollerController;
		obj.onDataChanged = (Action)Delegate.Remove(obj.onDataChanged, new Action(UpdateFilterBadge));
	}

	public void OpenBusiness(BusinessCellView.BusinessModel businessModel)
	{
		InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(businessModel.Address);
	}

	public void OpenResidential(PrivateResidenceCellView.PrivateResidenceModel privateResidenceModel)
	{
		InstanceBehavior<UIs>.Instance.fullMenu.bizMan.Open(privateResidenceModel.Address, "Presentation");
	}

	public void ToggleFilterPanel()
	{
		bizManFilterPanel.Toggle();
	}

	private void UpdateFilterBadge()
	{
		filterBadge.UpdateBadge(businessScrollerController.ActiveFilterCount);
	}
}
