using BigAmbitions.Items;
using Entities;

namespace Controllers;

public class WorkstationController : DesktopWorkstationController
{
	private ItemController _chairItemController;

	public ItemController FindChair(bool forceCheck = false)
	{
		if (forceCheck)
		{
			_chairItemController = null;
			employeeChair = null;
		}
		if (employeeChair != null && _chairItemController == null)
		{
			_chairItemController = employeeChair.GetComponent<ItemController>();
			return _chairItemController;
		}
		if (_chairItemController != null)
		{
			if (_chairItemController.Occupied)
			{
				return _chairItemController;
			}
			_chairItemController = null;
		}
		employeeChair = null;
		ItemController itemController = this;
		while ((bool)itemController.parentItemController)
		{
			itemController = itemController.parentItemController;
		}
		CheckForChair(itemController);
		return _chairItemController;
		bool CheckForChair(ItemController ic)
		{
			if ((ic.Item.type & ItemType.Seat) != 0 && !ic.Occupied)
			{
				_chairItemController = ic;
				employeeChair = ic.SittingPosition;
				return true;
			}
			foreach (ItemController childItemController in ic.childItemControllers)
			{
				if (CheckForChair(childItemController))
				{
					return true;
				}
			}
			return false;
		}
	}

	public override void AssignEmployee(ThirdPersonCharacter tpc, EmployeeInstance employeeInstance)
	{
		ItemController itemController = FindChair();
		if ((bool)itemController)
		{
			itemController.Occupied = true;
			if (runTypingAnimation)
			{
				PlayVideoOnScreen(VideoClipData.VideoType.Work);
			}
			base.AssignEmployee(tpc, employeeInstance);
		}
	}

	public override void UnassignEmployee()
	{
		_chairItemController.Occupied = false;
		StopVideoOnScreen();
		base.UnassignEmployee();
	}

	protected override void OnParentNewChild(ItemController newChild)
	{
		if (newChild is SeatController)
		{
			FindChair(forceCheck: true);
		}
	}

	protected override void OnParentChildRemoved(ItemController removedChild)
	{
		if (removedChild == _chairItemController)
		{
			FindChair(forceCheck: true);
		}
	}
}
