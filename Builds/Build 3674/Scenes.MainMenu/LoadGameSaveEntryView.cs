using System;
using Localizor.LanguageChangeEvent;
using TMPro;
using Tooltip;
using UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace Scenes.MainMenu;

public class LoadGameSaveEntryView : MonoBehaviour
{
	[SerializeField]
	private Button rowButton;

	[SerializeField]
	private Image iconImage;

	[SerializeField]
	private TMP_Text nameLabel;

	[SerializeField]
	private TMP_Text lastPlayedLabel;

	[SerializeField]
	private TextLocalizationComponent dayLabel;

	[SerializeField]
	private Button loadGameButton;

	[SerializeField]
	private GameObject deleteSaveButtonContainer;

	[SerializeField]
	private Button deleteSaveButton;

	[SerializeField]
	private ModTooltip modTooltip;

	[SerializeField]
	private ImageTooltip imageTooltip;

	public void Setup(SaveGameManager.SaveGameStruct saveGame, Sprite icon, Action onLoad, Action onDelete)
	{
		iconImage.sprite = icon;
		nameLabel.text = saveGame.name;
		lastPlayedLabel.text = saveGame.lastPlayedDate.ToShortDateString() + " " + saveGame.lastPlayedDate.Hour.GetFormattedTime(saveGame.lastPlayedDate.Minute);
		dayLabel.Arguments = new
		{
			day = "common_day",
			dayNumber = saveGame.day
		};
		modTooltip.Setup(saveGame);
		imageTooltip.Setup(icon);
		rowButton.onClick.RemoveAllListeners();
		rowButton.onClick.AddListener(delegate
		{
			onLoad?.Invoke();
		});
		loadGameButton.onClick.RemoveAllListeners();
		loadGameButton.onClick.AddListener(delegate
		{
			onLoad?.Invoke();
		});
		if (deleteSaveButton == null)
		{
			return;
		}
		deleteSaveButton.onClick.RemoveAllListeners();
		if (onDelete != null)
		{
			deleteSaveButton.onClick.AddListener(delegate
			{
				onDelete();
			});
		}
	}
}
