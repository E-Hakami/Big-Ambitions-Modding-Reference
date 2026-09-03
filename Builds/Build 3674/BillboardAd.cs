using System;
using Entities;
using UnityEngine;

public class BillboardAd : MonoBehaviour
{
	private static MaterialPropertyBlock _propertyBlock;

	private static readonly int EmissionTextureId = Shader.PropertyToID("_EmissionTexture");

	[SerializeField]
	private int materialIndex;

	[SerializeField]
	private MarketingTypeName size;

	[SerializeField]
	private LogoSize logoSize;

	[SerializeField]
	private Renderer billboardRenderer;

	private float _nextTimeToRequestInMinutes;

	public AdSettings currentAdSettings;

	private static MaterialPropertyBlock PropertyBlock => _propertyBlock ?? (_propertyBlock = new MaterialPropertyBlock());

	private void Awake()
	{
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool toggled)
		{
			if (toggled)
			{
				HideBillboard();
			}
			else
			{
				ShowBillboard();
			}
		});
	}

	public void DoUpdate(float currentTimeInMinutes)
	{
		if (!(_nextTimeToRequestInMinutes > currentTimeInMinutes))
		{
			ShowNextAd((size == MarketingTypeName.None) ? InstanceBehavior<AdManager>.Instance.RequestAd(logoSize, out _nextTimeToRequestInMinutes, this) : InstanceBehavior<AdManager>.Instance.RequestAd(size, out _nextTimeToRequestInMinutes, this));
		}
	}

	public void ShowNextAd(AdSettings settings)
	{
		if (settings == null)
		{
			return;
		}
		currentAdSettings = settings;
		LogoSize logoSize = ((size == MarketingTypeName.None) ? this.logoSize : LogoSize.Billboard);
		Texture2D businessLogoTexture = LogoHelper.GetBusinessLogoTexture(settings.businessName, logoSize, settings.isPlayerAd);
		if (businessLogoTexture != null)
		{
			SetTexture(businessLogoTexture);
		}
		else
		{
			if (!settings.isPlayerAd)
			{
				return;
			}
			BuildingRegistration registration = SaveGameManager.Current.BuildingRegistrations.Find((BuildingRegistration x) => x.BusinessName == settings.businessName);
			if (registration == null)
			{
				return;
			}
			BusinessLogoGenerator.Create(registration.BusinessName, registration.logoSettings, LogoHelper.GetPlayerBusinessLogoPath(registration.BusinessName), settings.isPlayerAd, delegate
			{
				Texture2D businessLogoTexture2 = LogoHelper.GetBusinessLogoTexture(registration.BusinessName, LogoSize.SquareSign, playerBusiness: true);
				if (businessLogoTexture2 != null)
				{
					SetTexture(businessLogoTexture2);
				}
			});
		}
	}

	private void SetTexture(Texture2D texture)
	{
		billboardRenderer.GetPropertyBlock(PropertyBlock, materialIndex);
		PropertyBlock.SetTexture(EmissionTextureId, texture);
		billboardRenderer.SetPropertyBlock(PropertyBlock, materialIndex);
	}

	private void ShowBillboard()
	{
		billboardRenderer.enabled = true;
	}

	private void HideBillboard()
	{
		billboardRenderer.enabled = false;
	}
}
