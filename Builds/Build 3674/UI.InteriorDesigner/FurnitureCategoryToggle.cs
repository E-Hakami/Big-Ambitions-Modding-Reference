using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.InteriorDesigner;

public class FurnitureCategoryToggle : MonoBehaviour
{
	public static FurnitureCategoryToggle CurrentActiveToggle;

	[SerializeField]
	private FurnitureActionPanelUi furniturePanel;

	[SerializeField]
	private Toggle toggle;

	[SerializeField]
	public List<string> includedTags;

	[SerializeField]
	public List<string> excludedTags;

	private void Awake()
	{
		toggle.onValueChanged.AddListener(OnValueChanged);
	}

	private void OnValueChanged(bool isOn)
	{
		if (isOn)
		{
			furniturePanel.ShowCategory(includedTags, excludedTags);
			CurrentActiveToggle = this;
		}
	}
}
