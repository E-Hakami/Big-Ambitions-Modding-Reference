using System;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan;

public class DeliveryContractEntry : MonoBehaviour
{
	[SerializeField]
	private Image image;

	[SerializeField]
	private Image businessLogo;

	[SerializeField]
	private Button button;

	[SerializeField]
	private TMP_Text businessNameText;

	[SerializeField]
	private TMP_Text deliveryFeeText;

	[SerializeField]
	private GameObject warningSign;

	private static readonly Color UnselectedColor = new Color(1f, 1f, 1f, 0.5f);

	private Sprite _createdSprite;

	public void Initialize(DeliveryContract contract, Action<DeliveryContractEntry, DeliveryContract> selectContract)
	{
		DestroyLogoSprite();
		UpdateWarningSign(!contract.HasItemsToDeliver());
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(contract.wholesaleAddress);
		Texture2D businessLogoTexture = LogoHelper.GetBusinessLogoTexture(buildingRegistration.BusinessName, LogoSize.SquareSign);
		Rect rect = new Rect(0f, 0f, businessLogoTexture.width, businessLogoTexture.height);
		_createdSprite = Sprite.Create(businessLogoTexture, rect, new Vector2(0.5f, 0.5f));
		businessLogo.sprite = _createdSprite;
		image.color = UnselectedColor;
		button.onClick.AddListener(delegate
		{
			selectContract(this, contract);
		});
		businessNameText.color = Color.white;
		businessNameText.text = buildingRegistration.BusinessName;
		deliveryFeeText.color = Color.white;
		deliveryFeeText.text = "bizman_delivery_contract_delivery_fee".Localize(new
		{
			fee = contract.deliveryFee.ToCurrencyFormat()
		}).ToString();
		base.gameObject.SetActive(value: true);
	}

	public void UpdateWarningSign(bool shouldShow)
	{
		warningSign.SetActive(shouldShow);
	}

	private void OnDestroy()
	{
		DestroyLogoSprite();
	}

	private void DestroyLogoSprite()
	{
		if ((bool)_createdSprite)
		{
			businessLogo.sprite = null;
			UnityEngine.Object.Destroy(_createdSprite);
		}
	}

	public void SetSelected(bool isSelected)
	{
		if (isSelected)
		{
			image.color = Color.white;
			businessNameText.color = Color.black;
			deliveryFeeText.color = Color.black;
		}
		else
		{
			image.color = UnselectedColor;
			businessNameText.color = Color.white;
			deliveryFeeText.color = Color.white;
		}
	}
}
