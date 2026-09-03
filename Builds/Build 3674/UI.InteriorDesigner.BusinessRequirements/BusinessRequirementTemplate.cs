using Localizor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.InteriorDesigner.BusinessRequirements;

public class BusinessRequirementTemplate : MonoBehaviour
{
	[SerializeField]
	private TMP_Text itemTitle;

	[SerializeField]
	private Toggle requirementMetToggle;

	private string _helpLink;

	public void SetUp(string localizeKey, bool isRequirementMet, string helpLink)
	{
		itemTitle.text = localizeKey.GetLocalization();
		requirementMetToggle.isOn = isRequirementMet;
		_helpLink = helpLink;
	}

	public void OnHelpButtonClicked()
	{
		if (!string.IsNullOrEmpty(_helpLink))
		{
			InstanceBehavior<HelpSystem>.Instance.OpenLink(_helpLink);
			InstanceBehavior<HelpSystem>.Instance.Toggle(show: true);
		}
	}
}
