using Localizor;
using TMPro;
using UnityEngine;

namespace UI.Smartphone.Apps.BizMan.StartBusiness;

public class StartBusinessInventorySourceEntry : MonoBehaviour
{
	[SerializeField]
	private TMP_Text productSourcesText;

	[Header("Requirements Fulfilled")]
	[SerializeField]
	private GameObject fulfilledIcon;

	[SerializeField]
	private Color fulfilledTextColor;

	[Header("Requirements Missing")]
	[SerializeField]
	private GameObject missingIcon;

	[SerializeField]
	private Color missingTextColor;

	public void Initialize(string productSource, bool hasRequirements)
	{
		productSourcesText.text = productSource.GetLocalization();
		productSourcesText.color = (hasRequirements ? fulfilledTextColor : missingTextColor);
		fulfilledIcon.SetActive(hasRequirements);
		missingIcon.SetActive(!hasRequirements);
		base.gameObject.SetActive(value: true);
	}
}
