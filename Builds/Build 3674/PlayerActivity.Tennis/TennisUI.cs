using System;
using Extensions;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerActivity.Tennis;

public class TennisUI : MonoBehaviour
{
	public TennisScoreLine[] scoreLines;

	public Action onPlayAgain;

	public Action onQuit;

	[SerializeField]
	private GameObject playAgainPrompt;

	[SerializeField]
	private TextLocalizationComponent playAgainPromptText;

	[SerializeField]
	private Button playAgainButton;

	[SerializeField]
	private Button quitButton;

	[SerializeField]
	private TextLocalizationComponent playAgainButtonText;

	[SerializeField]
	private AudioSource applauseSound;

	[SerializeField]
	private TennisPopup popup;

	private void Awake()
	{
		playAgainButton.onClick.AddListener(delegate
		{
			onPlayAgain?.Invoke();
		});
		quitButton.onClick.AddListener(delegate
		{
			onQuit?.Invoke();
		});
	}

	private void Start()
	{
		playAgainButtonText.Arguments = new
		{
			fee = 50f.ToShortCurrencyFormat()
		};
	}

	public void ShowNotification(string key)
	{
		popup.ShowPopup(key);
	}

	public void ShowNotification(bool isPositive, string key)
	{
		popup.ShowPopup(isPositive, key);
	}

	public bool IsPopupActive()
	{
		return popup.gameObject.activeSelf;
	}

	public void PlayApplause()
	{
		applauseSound.Play();
	}

	public void ShowPlayAgainPrompt(bool playerWon)
	{
		playAgainPrompt.SetActive(value: true);
		playAgainPromptText.Key = (playerWon ? "ba:tennisui_match_won" : "ba:tennisui_match_lost");
	}

	public void OnResetMatch()
	{
		playAgainPrompt.SetActive(value: false);
	}
}
