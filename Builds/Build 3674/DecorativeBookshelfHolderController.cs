using System.Linq;
using BigAmbitions.Items;
using Player.HUD.ItemInfoOverlays;

public class DecorativeBookshelfHolderController : DecorativeItemHolderController
{
	protected override void OnItemsInCargoUpdated()
	{
		if (InstanceBehavior<OverlayManager>.Instance.IsShowingOverlayOverItem(this))
		{
			InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
		}
		int num = base.ItemInstance.cargoInstances.Sum((CargoInstance x) => x.amount);
		for (int num2 = 0; num2 < itemVisualsParent.childCount; num2++)
		{
			itemVisualsParent.GetChild(num2).gameObject.SetActive(num2 < num);
		}
	}

	public override void FillVisualsOnly()
	{
		if (!(itemVisualsParent == null))
		{
			for (int i = 0; i < itemVisualsParent.childCount; i++)
			{
				itemVisualsParent.GetChild(i).gameObject.SetActive(value: true);
			}
		}
	}
}
