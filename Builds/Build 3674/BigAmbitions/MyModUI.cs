using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BigAmbitions;

public class MyModUI : SingleModUI
{
	[SerializeField]
	private TMP_Text titleLabel;

	[SerializeField]
	private TMP_Text versionLabel;

	[SerializeField]
	private Button editModButton;

	private Action<ulong, Button> _onEditModClick;

	public override void Setup(ModInfo modInfo)
	{
		base.Setup(modInfo);
		titleLabel.text = modInfo.title;
		versionLabel.text = GameVersion.GetVersionString(modInfo.targetBuildNumber);
		editModButton.interactable = true;
	}

	public void SetOnEditModClick(Action<ulong, Button> onEditModClick)
	{
		_onEditModClick = onEditModClick;
	}

	public void OnEditModClick()
	{
		_onEditModClick?.Invoke(currentSteamItemId, editModButton);
	}
}
