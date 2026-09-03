using BigAmbitions.Characters.Appearance;
using Helpers;
using UI;
using UnityEngine;

namespace PlayerActivity.Tennis;

public class TennisInteractionNpc : EntityController
{
	public TennisCourt court;

	public AppearanceSetter appearanceSetter;

	public override void Start()
	{
		if (TennisCourt.IsInTestScene())
		{
			base.gameObject.SetActive(value: false);
			return;
		}
		base.Start();
		court.SetLinkedInteractionNpc(this);
		appearanceSetter.SetRandomAppearance(new AppearanceTag[1] { AppearanceTag.Sport });
		SkinnedMeshRenderer combinedMeshSkinnedMeshRenderer = appearanceSetter.meshCombiner.GetCombinedMeshSkinnedMeshRenderer();
		Renderer[] array = new Renderer[renderers.Length + 1];
		for (int i = 0; i < renderers.Length; i++)
		{
			array[i] = renderers[i];
		}
		array[^1] = combinedMeshSkinnedMeshRenderer;
		renderers = array;
	}

	public override bool ShouldReactToIoEnter()
	{
		if (InstanceBehavior<UIs>.Instance.playerActivityUI.GetCurrentActivity == null)
		{
			return base.ShouldReactToIoEnter();
		}
		return false;
	}

	public override bool ShouldShowDetailedOverlay()
	{
		return GetClosestNavMeshTargetPosition(PlayerHelper.GetPosition()) != Vector3.zero;
	}

	public override void OnIoEnter()
	{
		if (!CityMap.IsOpen && !GameManager.IsAnyMiniGameActive())
		{
			base.OnIoEnter();
		}
	}

	public void PerformActivity()
	{
		court.PerformActivity();
	}
}
