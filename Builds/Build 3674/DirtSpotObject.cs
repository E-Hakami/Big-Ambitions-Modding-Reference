using Helpers;
using UnityEngine;

public class DirtSpotObject : EntityController
{
	[SerializeField]
	private Renderer dirt;

	private static MaterialPropertyBlock _propertyBlock;

	private static readonly int Dirt = Shader.PropertyToID("_Dirt");

	private static MaterialPropertyBlock PropertyBlock => _propertyBlock ?? (_propertyBlock = new MaterialPropertyBlock());

	public int DirtSpot => base.transform.GetSiblingIndex();

	protected override int DefaultLayer => LayerHelper.GroundLayerIndex;

	public override void Start()
	{
		base.Start();
		RandomizeDirtScaleAndRotation();
		UpdateNavMeshTargets();
	}

	public override void OnIoEnter()
	{
	}

	public override void OnIoExit()
	{
	}

	public override bool OnIoLeftClick()
	{
		if (!PlayerHelper.IsHoldingAMop)
		{
			return false;
		}
		return base.OnIoLeftClick();
	}

	private void RandomizeDirtScaleAndRotation()
	{
		Transform obj = dirt.transform;
		float num = Random.Range(1.2f, 1.6f);
		obj.localScale = new Vector3(num, num, 1f);
		obj.localRotation = Quaternion.Euler(obj.eulerAngles.x, 0f, Random.Range(0, 180));
	}

	public void SetDirtiness()
	{
		if (base.isActiveAndEnabled)
		{
			float num = ((InstanceBehavior<BuildingManager>.Instance.buildingRegistration.dirtSpots?.Count > DirtSpot) ? InstanceBehavior<BuildingManager>.Instance.buildingRegistration.dirtSpots[DirtSpot].dirtiness : 0f);
			dirt.enabled = visible && num >= 5f;
			if (dirt.enabled)
			{
				dirt.GetPropertyBlock(PropertyBlock);
				PropertyBlock.SetFloat(Dirt, num / 100f);
				dirt.SetPropertyBlock(PropertyBlock);
			}
		}
	}

	public void HideDirt()
	{
		dirt.enabled = false;
	}

	public void SetDirtinessVisibilityBasedOnHeight()
	{
		visible = InstanceBehavior<BuildingManager>.Instance.multipleHeightsBuildingController.GetPositionVisible(base.transform.position);
	}

	public override bool Interact()
	{
		InstanceBehavior<BuildingManager>.Instance.InteractFloorCell(this);
		return true;
	}
}
