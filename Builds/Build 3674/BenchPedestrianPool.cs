using JimmysUnityUtilities;
using UnityEngine;

[CreateAssetMenu(menuName = "BigAmbitions/AI/Pools/BenchPedestrianPool")]
public class BenchPedestrianPool : BaseHumanPool
{
	protected override void InitHuman(BaseHuman human)
	{
		base.InitHuman(human);
		human.SitOnChair(null);
		human.RemoveComponent<HumanHider>();
	}
}
