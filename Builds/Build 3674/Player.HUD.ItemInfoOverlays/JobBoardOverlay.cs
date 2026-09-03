using UnityEngine;
using UnityEngine.UI;

namespace Player.HUD.ItemInfoOverlays;

public class JobBoardOverlay : IOverlay
{
	[Header("Jobboard InfoOverlay")]
	[SerializeField]
	private Button viewCandidatesButton;

	[SerializeField]
	private Button changeTextButton;

	public override bool IsValid(EntityController entityController)
	{
		return entityController is JobBoardController;
	}

	public override bool ShouldShow(EntityController entityController)
	{
		return InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness;
	}

	public override void UpdateOverlay(EntityController entityController)
	{
		viewCandidatesButton.onClick.RemoveAllListeners();
		viewCandidatesButton.onClick.AddListener(OnShowCandidatesClicked);
		changeTextButton.onClick.RemoveAllListeners();
		changeTextButton.onClick.AddListener(OnChangeTextClicked);
	}

	private void OnShowCandidatesClicked()
	{
		if (linkedController is JobBoardController jobBoardController)
		{
			jobBoardController.ShowCandidates();
		}
		InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
	}

	private void OnChangeTextClicked()
	{
		if (linkedController is JobBoardController jobBoardController)
		{
			jobBoardController.OpenChangeTextOverlayInInteriorDesigner();
		}
		InstanceBehavior<OverlayManager>.Instance.HideDetailedOverlay();
	}
}
