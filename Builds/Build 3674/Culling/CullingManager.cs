namespace Culling;

public class CullingManager : InstanceBehavior<CullingManager>
{
	private const float FirstBand = 60f;

	private const float SecondBand = 200f;

	public CullingGroupController generalCullingGroupController;

	public CullingGroupController hamptonsHousesCullingGroupController;

	protected override void Awake()
	{
		base.Awake();
		generalCullingGroupController = new CullingGroupController(60f, 200f);
		hamptonsHousesCullingGroupController = new CullingGroupController(60f, 200f);
	}

	private void LateUpdate()
	{
		generalCullingGroupController.Update();
		hamptonsHousesCullingGroupController.Update();
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		generalCullingGroupController.Dispose();
		hamptonsHousesCullingGroupController.Dispose();
	}
}
