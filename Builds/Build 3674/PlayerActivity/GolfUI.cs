using System;
using DG.Tweening;
using Extensions;
using Localizor.LanguageChangeEvent;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerActivity;

public class GolfUI : MonoBehaviour
{
	[SerializeField]
	private Button forfeitBallButton;

	[SerializeField]
	private TextMeshProUGUI scoreText;

	[SerializeField]
	private TextMeshProUGUI ballsLeftText;

	[SerializeField]
	private Image powerBarFill;

	[SerializeField]
	private GameObject playAgainPrompt;

	[SerializeField]
	private Button playAgainButton;

	[SerializeField]
	private Button quitButton;

	[SerializeField]
	private TextLocalizationComponent playAgainButtonText;

	[SerializeField]
	private GameObject windPanel;

	[SerializeField]
	private Image windIcon;

	[SerializeField]
	private RawImage windIndicator;

	[SerializeField]
	private float windIndicatorSpeed = 1f;

	[SerializeField]
	private CanvasGroup windWarningCg;

	[SerializeField]
	private float windWarningDuration = 4f;

	[SerializeField]
	private float windWarningFadeOutDuration = 1f;

	[NonSerialized]
	public float playFee;

	public Action onForfeitBall;

	public Action onPlayAgain;

	public Action onQuit;

	private void Awake()
	{
		forfeitBallButton.onClick.AddListener(delegate
		{
			onForfeitBall?.Invoke();
		});
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
			fee = playFee.ToShortCurrencyFormat()
		};
	}

	private void Update()
	{
		if (windPanel.activeSelf)
		{
			Rect uvRect = windIndicator.uvRect;
			uvRect.x = Mathf.Repeat(uvRect.x - Time.deltaTime * windIndicatorSpeed, 1f);
			windIndicator.uvRect = uvRect;
		}
	}

	public void SetForfeitBallButtonActive(bool active)
	{
		forfeitBallButton.gameObject.SetActive(active);
	}

	public void UpdateScore(int score)
	{
		scoreText.text = score.ToString();
	}

	public void UpdateBallsLeft(int ballsLeft)
	{
		ballsLeftText.text = ballsLeft.ToString();
		playAgainPrompt.SetActive(ballsLeft <= 0);
	}

	public void SetPowerBarActive(bool active)
	{
		if (powerBarFill.gameObject.activeSelf != active)
		{
			powerBarFill.gameObject.SetActive(active);
		}
	}

	public void UpdatePowerBar(float powerNormalized)
	{
		powerBarFill.fillAmount = powerNormalized;
	}

	public void OnShotSubmit()
	{
		windWarningCg.gameObject.SetActive(value: false);
		windWarningCg.DOKill();
	}

	public void UpdateWind(float wind)
	{
		bool flag = wind != 0f;
		if (windPanel.activeSelf != flag)
		{
			windPanel.SetActive(flag);
		}
		if (flag)
		{
			Vector3 one = Vector3.one;
			one.x = Mathf.Sign(wind);
			windIndicator.transform.localScale = one;
			windIcon.transform.localScale = one;
			windWarningCg.gameObject.SetActive(value: true);
			windWarningCg.DOKill();
			windWarningCg.alpha = 1f;
			windWarningCg.DOFade(0f, windWarningFadeOutDuration).SetDelay(windWarningDuration).SetUpdate(isIndependentUpdate: true)
				.OnComplete(delegate
				{
					windWarningCg.gameObject.SetActive(value: false);
				});
		}
	}
}
