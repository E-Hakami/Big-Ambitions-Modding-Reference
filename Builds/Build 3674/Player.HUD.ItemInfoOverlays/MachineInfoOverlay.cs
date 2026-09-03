using System.Linq;
using Items.SpecialItems;
using Localizor.LanguageChangeEvent;
using UnityEngine;

namespace Player.HUD.ItemInfoOverlays;

public class MachineInfoOverlay : IOverlay
{
	private const string OutputToPalletShelvesKey = "factory_overlay_output_to_pallet_shelves";

	private const string NotEnoughSpaceKey = "bizman_factory_inactive_reason_no_space";

	[Header("Machine")]
	[SerializeField]
	private TextLocalizationComponent infoTextComponent;

	public override bool IsValid(EntityController entityController)
	{
		return entityController is FactoryAssemblyMachineController;
	}

	public override bool ShouldShow(EntityController entityController)
	{
		return entityController as FactoryAssemblyMachineController != null;
	}

	public override void UpdateOverlay(EntityController entityController)
	{
		if (entityController is FactoryAssemblyMachineController factoryAssemblyMachineController)
		{
			if (factoryAssemblyMachineController.WorkstationInstance.IsSpaceOnPalletShelves(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.itemInstances.Values.ToList()))
			{
				infoTextComponent.Key = "factory_overlay_output_to_pallet_shelves";
				infoTextComponent.TextContainer.color = InstanceBehavior<GlobalReferences>.Instance.colors.white;
			}
			else
			{
				infoTextComponent.Key = "bizman_factory_inactive_reason_no_space";
				infoTextComponent.TextContainer.color = InstanceBehavior<GlobalReferences>.Instance.colors.red;
			}
		}
	}
}
