using System;
using Buildings.Office.Headquarters;
using TMPro;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.PricingManagers;

public class PricingManagersPlanListEntry : MonoBehaviour
{
	private const float UnassignedNameMargin = 50f;

	[SerializeField]
	private GameObject notSelectedRoot;

	[SerializeField]
	private Button notSelectedButton;

	[SerializeField]
	private GameObject unassignedManagerIcon;

	[SerializeField]
	private TMP_Text managerNameLabel;

	[SerializeField]
	private TMP_Text neighborhoodNameNotSelectedLabel;

	[SerializeField]
	private GameObject selectedRoot;

	[SerializeField]
	private TMP_Text neighborhoodNameSelectedLabel;

	[SerializeField]
	private UI.Elements.Dropdown managerDropdown;

	public PricingManagerPlan Plan { get; private set; }

	public UI.Elements.Dropdown ManagerDropdown => managerDropdown;

	public void Initialize(PricingManagerPlan plan, Action<PricingManagersPlanListEntry> onSelected)
	{
		Plan = plan;
		base.name = plan.id;
		notSelectedButton.onClick.RemoveAllListeners();
		notSelectedButton.onClick.AddListener(delegate
		{
			onSelected?.Invoke(this);
		});
	}

	public void SetNeighborhoodName(string neighborhoodName)
	{
		neighborhoodNameNotSelectedLabel.text = neighborhoodName;
		neighborhoodNameSelectedLabel.text = neighborhoodName;
	}

	public void SetManager(string managerName, bool hasManager)
	{
		managerNameLabel.text = managerName;
		unassignedManagerIcon.SetActive(!hasManager);
		managerNameLabel.margin = new Vector4(hasManager ? 0f : 50f, 0f, 0f, 0f);
	}

	public void SetSelected(bool isSelected)
	{
		selectedRoot.SetActive(isSelected);
		notSelectedRoot.SetActive(!isSelected);
	}
}
