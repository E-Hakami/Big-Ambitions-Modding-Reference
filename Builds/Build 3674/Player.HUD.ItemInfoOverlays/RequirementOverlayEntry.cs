using BigAmbitions.Items;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace Player.HUD.ItemInfoOverlays;

public class RequirementOverlayEntry : MonoBehaviour
{
	[SerializeField]
	private Toggle toggle;

	[SerializeField]
	private TextLocalizationComponent requirementText;

	[SerializeField]
	private GameObject dash;

	public Toggle Toggle => toggle;

	public void Initialize(FurnitureRequirement requirement, bool showToggle)
	{
		requirementText.Key = requirement.localizationKey;
		requirementText.Arguments = requirement.localizationArguments;
		toggle.gameObject.SetActive(showToggle);
		base.gameObject.SetActive(value: true);
		dash.SetActive(!showToggle);
	}
}
