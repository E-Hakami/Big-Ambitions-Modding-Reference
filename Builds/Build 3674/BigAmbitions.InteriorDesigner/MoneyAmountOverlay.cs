using Extensions;
using TMPro;
using UnityEngine;

namespace BigAmbitions.InteriorDesigner;

public class MoneyAmountOverlay : MonoBehaviour
{
	[SerializeField]
	private UIHoverAboveObject hoverAboveObject;

	[SerializeField]
	private TMP_Text priceText;

	public bool isOpen;

	public void Open(Transform elementTransform, float price)
	{
		hoverAboveObject.SetObjectToFollow(elementTransform);
		priceText.text = price.ToShortCurrencyFormat();
		base.gameObject.SetActive(value: true);
		isOpen = true;
	}

	public void Close()
	{
		if (isOpen)
		{
			isOpen = false;
			base.gameObject.SetActive(value: false);
		}
	}
}
