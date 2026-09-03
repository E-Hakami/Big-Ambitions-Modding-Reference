using Buildings;
using Buildings.Indoors.InteriorDesign;
using Extensions;
using Helpers;
using Player.HUD.ItemWarningIcons;
using UI;
using UnityEngine;

public class ScreenCinemaController : ItemController
{
	[SerializeField]
	private Transform projector;

	[SerializeField]
	private VideoClipData.VideoType videoType = VideoClipData.VideoType.TV;

	private bool _isPlayingVideo;

	public override void Start()
	{
		base.Start();
		SetProjectorPosition();
		InvokeRepeating("UpdateScreen", Random.Range(0f, 0.5f), 1f);
	}

	public override void OnItemPositionUpdated()
	{
		base.OnItemPositionUpdated();
		SetProjectorPosition();
	}

	private void SetProjectorPosition()
	{
		string buildingSize = ((BuildingPreview.isPreviewing && InstanceBehavior<UIs>.Instance != null) ? InstanceBehavior<UIs>.Instance.buildingPreview.GetCurrentBuildingSize : base.BuildingContext.Building.BuildingSize);
		MultipleHeightsBuildingController multipleHeightsBuildingController = ((BuildingPreview.isPreviewing && InstanceBehavior<UIs>.Instance != null) ? InstanceBehavior<UIs>.Instance.buildingPreview.GetMultipleHeightsBuildingController : base.BuildingContext.MultipleHeights);
		projector.SetPositionY((multipleHeightsBuildingController != null) ? multipleHeightsBuildingController.GetCeilingYPositionForRoofObject(base.transform.position, multipleHeightsBuildingController.GetItemHeightIndex(this)) : BuildingSizeHelper.GetBuildingRoofPosition(buildingSize, 0));
	}

	private void UpdateScreen()
	{
		if (base.BuildingContext.IsPlayerOwnedBusiness && (bool)InstanceBehavior<ItemWarningIconManager>.Instance)
		{
			InstanceBehavior<ItemWarningIconManager>.Instance.UpdateWarningIcon(this);
		}
		BuildingRegistration registration = base.BuildingContext.Registration;
		bool flag = !InteriorDesignerHelper.BlueprintCreatorMode && registration != null && registration.businessTypeName == "ba:businesstype_cinema" && BusinessHelper.IsBusinessOpen(registration) && (!registration.RentedByPlayer || !ItemHelper.HasAnyMissingRequirements(base.ItemInstance));
		if (_isPlayingVideo != flag)
		{
			_isPlayingVideo = flag;
			if (flag)
			{
				PlayVideoOnScreen(videoType);
			}
			else
			{
				StopVideoOnScreen();
			}
		}
	}
}
