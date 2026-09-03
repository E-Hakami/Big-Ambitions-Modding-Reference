using BigAmbitions.InteriorDesigner.Tools;
using Items.SpecialItems;
using Player.HUD.ItemInfoOverlays;
using UnityEngine;

namespace BigAmbitions.InteriorDesigner;

public class FactoryMachineProducerOverlay : IProducerOverlay
{
	[SerializeField]
	private MachineOverlay machineOverlay;

	[SerializeField]
	private MachineInfoOverlay machineInfoOverlay;

	private string _originalWorkstationRecipeId;

	private string _originalWorkstationTypeId;

	private FactoryWorkstationInstance _workstationInstance;

	public override bool HasChanges()
	{
		if (_workstationInstance != null)
		{
			if (!(_originalWorkstationRecipeId != _workstationInstance.selectedRecipeId))
			{
				return _originalWorkstationTypeId != _workstationInstance.workstationType;
			}
			return true;
		}
		return false;
	}

	public override bool ShouldShow(ItemController itemController)
	{
		if (itemController is FactoryAssemblyMachineController && machineOverlay.IsValid(itemController) && machineOverlay.ShouldShow(itemController) && machineInfoOverlay.IsValid(itemController))
		{
			return machineInfoOverlay.ShouldShow(itemController);
		}
		return false;
	}

	public override void OnOpen(ItemController itemController)
	{
		FactoryAssemblyMachineController factoryAssemblyMachineController = itemController as FactoryAssemblyMachineController;
		if (factoryAssemblyMachineController == null)
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		_workstationInstance = factoryAssemblyMachineController.WorkstationInstance;
		_originalWorkstationRecipeId = factoryAssemblyMachineController.WorkstationInstance.selectedRecipeId;
		_originalWorkstationTypeId = factoryAssemblyMachineController.WorkstationInstance.workstationType;
		machineOverlay.UpdateOverlay(itemController);
		machineInfoOverlay.UpdateOverlay(itemController);
		machineOverlay.gameObject.SetActive(value: true);
		machineInfoOverlay.gameObject.SetActive(value: true);
		base.gameObject.SetActive(value: true);
	}

	public override void ExecuteRevertibleAction()
	{
		IInteriorDesignerTool.executeActionThroughCode(new ProducerFactoryMachineRevertibleAction(IProducerOverlay.currentItemIndex, _originalWorkstationRecipeId, _workstationInstance.selectedRecipeId, _originalWorkstationTypeId, _workstationInstance.workstationType));
	}
}
