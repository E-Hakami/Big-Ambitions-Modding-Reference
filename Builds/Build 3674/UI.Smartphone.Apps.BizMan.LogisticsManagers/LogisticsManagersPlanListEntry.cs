using System;
using Buildings.Office.Headquarters;
using TMPro;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.BizMan.LogisticsManagers;

public class LogisticsManagersPlanListEntry : MonoBehaviour
{
	[SerializeField]
	private GameObject notSelectedRoot;

	[SerializeField]
	private Button notSelectedButton;

	[SerializeField]
	private GameObject unassignedManagerIcon;

	[SerializeField]
	private TMP_Text managerNameLabel;

	[SerializeField]
	private TMP_Text locationNameNotSelectedLabel;

	[SerializeField]
	private GameObject selectedRoot;

	[SerializeField]
	private TMP_Text locationNameSelectedLabel;

	[SerializeField]
	private UI.Elements.Dropdown managerDropdown;

	public LogisticsManagerPlan Plan { get; private set; }

	public UI.Elements.Dropdown ManagerDropdown => managerDropdown;

	public void Initialize(LogisticsManagerPlan plan, Action<LogisticsManagersPlanListEntry> onSelected)
	{
		Plan = plan;
		base.name = plan.id;
		notSelectedButton.onClick.RemoveAllListeners();
		notSelectedButton.onClick.AddListener(delegate
		{
			onSelected?.Invoke(this);
		});
	}

	public void SetLocationName(string locationName)
	{
		locationNameNotSelectedLabel.text = locationName;
		locationNameSelectedLabel.text = locationName;
	}

	public void SetManager(string managerName, bool hasManager)
	{
		managerNameLabel.text = managerName;
		unassignedManagerIcon.SetActive(!hasManager);
		managerNameLabel.margin = new Vector4((!hasManager) ? 50 : 0, 0f, 0f, 0f);
	}

	public void SetSelected(bool isSelected)
	{
		selectedRoot.SetActive(isSelected);
		notSelectedRoot.SetActive(!isSelected);
	}
}
