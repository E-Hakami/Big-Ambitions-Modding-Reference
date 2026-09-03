using Helpers;

public class TrashBinController : ItemController
{
	public void DiscardItemInHand()
	{
		MoveTowardsEntity(delegate
		{
			HudConfirm.Show(null, "hud_confirm_discard_item", delegate
			{
				PlayerHelper.ItemInstanceInHands.Discard();
			});
		});
	}
}
