using PlayerActivity;
using UI.ItemPanel;
using UnityEngine;

namespace Controllers;

public class OutsideInteractableItemToRest : OutsideInteractableItem
{
	[SerializeField]
	private RestEnvironment restEnvironment;

	public override string GetCtaKey()
	{
		return "click_to_rest";
	}

	public override string GetItemOccupiedKey()
	{
		return customOverlayHeaderKey;
	}

	public override void PerformActivity()
	{
		if (!ItemPanelUI.IsVisible)
		{
			if (Occupied)
			{
				ShowOccupiedNotification();
			}
			else
			{
				PlayerActivityUI.Show(this, this);
			}
		}
	}

	public override IPlayerActivity CreateActivity(EntityController attachedEntity)
	{
		return restEnvironment.CreateActivity(attachedEntity);
	}
}
