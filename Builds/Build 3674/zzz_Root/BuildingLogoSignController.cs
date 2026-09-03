using UnityEngine;

public class BuildingLogoSignController : MonoBehaviour
{
	[SerializeField]
	private Renderer logoSignRenderer;

	private static MaterialPropertyBlock _propertyBlock;

	private static readonly int BaseColorMapId = Shader.PropertyToID("_BaseColorMap");

	private static MaterialPropertyBlock PropertyBlock => _propertyBlock ?? (_propertyBlock = new MaterialPropertyBlock());

	public void UpdateSign(BuildingRegistration registration)
	{
		base.gameObject.SetActive(value: true);
		Texture2D texture2D = (registration.AvailableForRent ? LogoHelper.GetBusinessLogoTexture("AvailableForRent", LogoSize.SquareSign) : LogoHelper.GetBusinessLogoTexture(registration.BusinessName, LogoSize.SquareSign, registration.RentedByPlayer));
		if (texture2D != null)
		{
			SetSignTexture(texture2D);
		}
		else
		{
			if (registration.AvailableForRent || !registration.RentedByPlayer)
			{
				return;
			}
			BusinessLogoGenerator.Create(registration.BusinessName, registration.logoSettings, LogoHelper.GetPlayerBusinessLogoPath(registration.BusinessName), registration.RentedByPlayer, delegate
			{
				Texture2D businessLogoTexture = LogoHelper.GetBusinessLogoTexture(registration.BusinessName, LogoSize.SquareSign, playerBusiness: true);
				if (businessLogoTexture != null)
				{
					SetSignTexture(businessLogoTexture);
				}
			});
		}
	}

	private void SetSignTexture(Texture2D texture)
	{
		logoSignRenderer.GetPropertyBlock(PropertyBlock, 1);
		PropertyBlock.SetTexture(BaseColorMapId, texture);
		logoSignRenderer.SetPropertyBlock(PropertyBlock, 1);
	}
}
