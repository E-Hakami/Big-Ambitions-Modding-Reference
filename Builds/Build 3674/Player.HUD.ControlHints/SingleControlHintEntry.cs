using System;
using Localizor;
using UnityEngine;

namespace Player.HUD.ControlHints;

public class SingleControlHintEntry : MonoBehaviour
{
	[SerializeField]
	private InlineControlsHintRenderer hintRenderer;

	private ControlsHint _hint;

	private void OnEnable()
	{
		LocalizorManager.OnLanguageChanged = (Action)Delegate.Combine(LocalizorManager.OnLanguageChanged, new Action(Refresh));
		Refresh();
	}

	private void OnDisable()
	{
		LocalizorManager.OnLanguageChanged = (Action)Delegate.Remove(LocalizorManager.OnLanguageChanged, new Action(Refresh));
	}

	public void SetHint(ControlsHint hint)
	{
		if (_hint != hint)
		{
			_hint = hint;
			if (base.isActiveAndEnabled)
			{
				Refresh();
			}
		}
	}

	private void Refresh()
	{
		if (_hint != null)
		{
			hintRenderer.SetContent(_hint.TextKey.GetLocalization(this), _hint.Bindings);
		}
	}
}
