using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("Big Ambitions/Casino")]
public class StopSlotMachineSounds : Action
{
	[RequiredField]
	public SharedItemController sharedSlotMachineItemController;

	public override void OnStart()
	{
		if (sharedSlotMachineItemController.Value is SlotMachineController slotMachineController)
		{
			slotMachineController.StopSlotMachineSounds();
		}
	}
}
