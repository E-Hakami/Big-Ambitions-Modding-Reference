using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu;

public class NewGamePanel : MonoBehaviour
{
	public const string StoryMode = "StoryMode";

	public const string CustomGameMode = "CustomGameMode";

	[SerializeField]
	private MainMenuController mainMenuController;

	[SerializeField]
	private Button startButton;

	[SerializeField]
	private List<NewGameModeButton> modeButtons;

	public static string GameMode { get; private set; }

	public static MainMenuController MainMenuController { get; private set; }

	private void Awake()
	{
		MainMenuController = mainMenuController;
	}

	private void Start()
	{
		ShowPanel("StoryMode");
	}

	public void ShowPanel(string mode)
	{
		GameMode = mode;
		foreach (NewGameModeButton modeButton in modeButtons)
		{
			modeButton.ShowPanel(modeButton.mode == mode);
		}
	}
}
