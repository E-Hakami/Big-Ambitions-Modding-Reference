using UnityEngine;

public class SinkController : HygieneItemController
{
	[SerializeField]
	private Renderer waterVisual;

	[SerializeField]
	private ParticleSystem vfx;

	[SerializeField]
	private Renderer vfxRenderer;

	private bool _enabled;

	public override void Show()
	{
		base.Show();
		if (_enabled)
		{
			ToggleEffectsRenderers(toggle: true);
		}
	}

	public override void Hide()
	{
		base.Hide();
		ToggleEffectsRenderers(toggle: false);
	}

	protected override void OnBeginUse(ThirdPersonCharacter tpc)
	{
		_enabled = true;
		vfx.Play();
		if (visible)
		{
			ToggleEffectsRenderers(toggle: true);
		}
	}

	protected override void OnEndUse(ThirdPersonCharacter tpc)
	{
		_enabled = false;
		ToggleEffectsRenderers(toggle: false);
		vfx.Stop();
	}

	private void ToggleEffectsRenderers(bool toggle)
	{
		waterVisual.enabled = toggle;
		vfxRenderer.enabled = toggle;
	}
}
