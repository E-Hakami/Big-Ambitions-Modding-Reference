using System.Collections.Generic;
using Extensions;
using UnityEngine;
using UnityEngine.UI;

namespace UI.MainMenu;

public class StoryPanel : MonoBehaviour
{
	[SerializeField]
	private Transform difficultyOptionTemplate;

	[SerializeField]
	private Button startButton;

	private readonly List<StoryDifficultyOption> _difficultyOptions = new List<StoryDifficultyOption>();

	private int _selectedDifficultyIndex = -1;

	private void Start()
	{
		_difficultyOptions.Clear();
		difficultyOptionTemplate.ResetTemplate();
		for (int i = 0; i < InstanceBehavior<GlobalReferences>.Instance.difficultySettings.Length; i++)
		{
			StoryDifficultyOption option = Object.Instantiate(difficultyOptionTemplate, difficultyOptionTemplate.parent).GetComponent<StoryDifficultyOption>();
			option.SetUp(InstanceBehavior<GlobalReferences>.Instance.difficultySettings[i], i);
			option.GetComponent<Button>()?.onClick.AddListener(delegate
			{
				SelectOption(option.Index);
			});
			_difficultyOptions.Add(option);
			option.gameObject.SetActive(value: true);
		}
		startButton.onClick.AddListener(StartGame);
	}

	private void OnEnable()
	{
		startButton.interactable = _selectedDifficultyIndex != -1;
	}

	private void SelectOption(int index)
	{
		_selectedDifficultyIndex = index;
		foreach (StoryDifficultyOption difficultyOption in _difficultyOptions)
		{
			difficultyOption.SetSelected(difficultyOption.Index == index);
		}
		startButton.interactable = true;
	}

	private void StartGame()
	{
		if (!(NewGamePanel.GameMode != "StoryMode"))
		{
			NewGamePanel.MainMenuController.StartNewGame(InstanceBehavior<GlobalReferences>.Instance.difficultySettings[_selectedDifficultyIndex].ToGameVariables());
		}
	}
}
