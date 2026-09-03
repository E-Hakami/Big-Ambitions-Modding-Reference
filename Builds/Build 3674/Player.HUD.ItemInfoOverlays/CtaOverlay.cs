using Controllers;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;

namespace Player.HUD.ItemInfoOverlays;

public class CtaOverlay : IOverlay
{
	[SerializeField]
	private TextLocalizationComponent ctaField;

	private string _ctaKey;

	private void Start()
	{
		ctaField.GetComponent<TMP_Text>().color = InstanceBehavior<GlobalReferences>.Instance.colors.blue;
	}

	public override bool IsValid(EntityController entityController)
	{
		return CtaManager.HasCta(entityController);
	}

	public override bool ShouldShow(EntityController entityController)
	{
		if (CityMap.IsOpen)
		{
			return false;
		}
		if (entityController is ItemController { playerItemPurchaserSettings: { enabled: not false } } itemController && !(itemController is ShowcaseVehicleController))
		{
			return false;
		}
		CtaManager.UpdateCta(entityController);
		_ctaKey = CtaManager.ctaKey;
		return !string.IsNullOrEmpty(_ctaKey);
	}

	public override void UpdateOverlay(EntityController entityController)
	{
		if (CtaManager.ctaArgs != null)
		{
			ctaField.SetData(CtaManager.ctaKey.Localize(CtaManager.ctaArgs));
		}
		else
		{
			ctaField.Key = _ctaKey;
		}
	}
}
