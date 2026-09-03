using System;
using System.Collections;
using System.Collections.Generic;
using BigAmbitions.SoundSystem;
using DG.Tweening;
using Extensions;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI.Dialog;

public class BlackjackUI : MonoBehaviour
{
	private static readonly Dictionary<string, string> TransactionData = new Dictionary<string, string> { { "element", "ba:itemname_casinoblackjacktable" } };

	private static readonly TransactionInfo TransactionInfo = new TransactionInfo("ba:transaction_casino", "ba:transactioncategory_casino", TransactionData);

	[SerializeField]
	private Transform dealerCardTemplate;

	[SerializeField]
	private TextLocalizationComponent dealerScoreLabel;

	[SerializeField]
	private Transform playerCardTemplate;

	[SerializeField]
	private TextLocalizationComponent playerScoreLabel;

	[SerializeField]
	private Button hitButton;

	[SerializeField]
	private Button standButton;

	[SerializeField]
	private Button insuranceButton;

	[SerializeField]
	private Button surrenderButton;

	[SerializeField]
	private List<Card> cardsDeck;

	private UnityAction<LanguageChangeEventDataHolder> _onFinished;

	private List<Card> _dealerCards;

	private List<Card> _playerCards;

	private float _initialBet;

	private bool _insuranceBought;

	private CasinoGameController _casinoGameController;

	private Func<bool> _isDialogActive;

	private void Start()
	{
		_dealerCards = new List<Card>();
		_playerCards = new List<Card>();
		dealerCardTemplate.gameObject.SetActive(value: false);
		dealerCardTemplate.GetComponent<Image>().SetAlpha(0f);
		dealerScoreLabel.Arguments = new
		{
			score = GetScore(_dealerCards)
		};
		playerCardTemplate.gameObject.SetActive(value: false);
		playerCardTemplate.GetComponent<Image>().SetAlpha(0f);
		playerScoreLabel.Arguments = new
		{
			score = GetScore(_playerCards)
		};
		ChangeAllButtons(state: false);
	}

	public void PlayBlackjack(UnityAction<LanguageChangeEventDataHolder> onFinished, float initialBet, CasinoGameController casinoGameController, Func<bool> isDialogActive)
	{
		_onFinished = onFinished;
		_initialBet = initialBet;
		_casinoGameController = casinoGameController;
		_isDialogActive = isDialogActive;
		CoroutineUtility.Run(GameStart());
	}

	private IEnumerator GameStart()
	{
		InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.BlackJackStart, _casinoGameController.transform.position);
		Card randomCard = GetRandomCard();
		_playerCards.Add(randomCard);
		DisplayCard(randomCard, playerCardTemplate);
		yield return new WaitForSeconds(1f);
		if (!IsDialogActive())
		{
			yield break;
		}
		playerScoreLabel.Arguments = new
		{
			score = GetScore(_playerCards)
		};
		Card randomCard2 = GetRandomCard();
		_playerCards.Add(randomCard2);
		DisplayCard(randomCard2, playerCardTemplate);
		yield return new WaitForSeconds(1f);
		if (!IsDialogActive())
		{
			yield break;
		}
		int score = GetScore(_playerCards);
		playerScoreLabel.Arguments = new { score };
		Card dealerCard = GetRandomCard();
		_dealerCards.Add(dealerCard);
		DisplayCard(dealerCard, dealerCardTemplate);
		yield return new WaitForSeconds(1f);
		if (!IsDialogActive())
		{
			yield break;
		}
		dealerScoreLabel.Arguments = new
		{
			score = GetScore(_dealerCards)
		};
		if (GetScore(_playerCards) == 21)
		{
			Card randomCard3 = GetRandomCard();
			_dealerCards.Add(randomCard3);
			DisplayCard(randomCard3, dealerCardTemplate);
			yield return new WaitForSeconds(1f);
			if (IsDialogActive())
			{
				int score2 = GetScore(_dealerCards);
				dealerScoreLabel.Arguments = new
				{
					score = score2
				};
				if (score2 == 21)
				{
					GameManager.ChangeMoneySafe(_initialBet, TransactionInfo);
					_onFinished("dialog_blackjack_both_blackjack".Localize());
					yield break;
				}
				float num = 1.5f * _initialBet;
				GameManager.ChangeMoneySafe(num + _initialBet, TransactionInfo);
				_casinoGameController.ShowVictoryAnimation();
				_onFinished("dialog_blackjack_player_blackjack".Localize(new
				{
					prize = num.ToShortCurrencyFormat()
				}));
			}
		}
		else
		{
			ChangeAllButtons(state: true);
			insuranceButton.interactable = CardIsAce(dealerCard);
		}
	}

	public void Hit()
	{
		ChangeAllButtons(state: false);
		CoroutineUtility.Run(DoHit());
	}

	private IEnumerator DoHit()
	{
		Card randomCard = GetRandomCard();
		_playerCards.Add(randomCard);
		DisplayCard(randomCard, playerCardTemplate);
		yield return new WaitForSeconds(1f);
		if (IsDialogActive())
		{
			int score = GetScore(_playerCards);
			playerScoreLabel.Arguments = new { score };
			if (score > 21)
			{
				_onFinished("dialog_casino_no_prize_dialog".Localize());
				_casinoGameController.ShowDefeatAnimation();
			}
			else if (score == 21)
			{
				CoroutineUtility.Run(DealerPlay());
			}
			else
			{
				hitButton.interactable = true;
				standButton.interactable = true;
			}
		}
	}

	public void Stand()
	{
		ChangeAllButtons(state: false);
		CoroutineUtility.Run(DealerPlay());
	}

	public void Insurance()
	{
		ChangeAllButtons(state: false);
		if (GameManager.ChangeMoneySafe((0f - _initialBet) / 2f, TransactionInfo, null, null, force: false, showNotification: true))
		{
			_insuranceBought = true;
		}
		hitButton.interactable = true;
		standButton.interactable = true;
	}

	public void Surrender()
	{
		ChangeAllButtons(state: false);
		GameManager.ChangeMoneySafe(_initialBet / 2f, TransactionInfo);
		_onFinished("dialog_blackjack_surrender_result".Localize());
	}

	private IEnumerator DealerPlay()
	{
		Card randomCard = GetRandomCard();
		_dealerCards.Add(randomCard);
		DisplayCard(randomCard, dealerCardTemplate);
		yield return new WaitForSeconds(1f);
		if (!IsDialogActive())
		{
			yield break;
		}
		int score = GetScore(_dealerCards);
		dealerScoreLabel.Arguments = new { score };
		if (score == 21)
		{
			if (_insuranceBought)
			{
				GameManager.ChangeMoneySafe(_initialBet, TransactionInfo);
				_onFinished("dialog_blackjack_dealer_blackjack_insurance".Localize());
			}
			else
			{
				_onFinished("dialog_blackjack_dealer_blackjack_no_insurance".Localize());
				_casinoGameController.ShowDefeatAnimation();
			}
			yield break;
		}
		while (score <= 16)
		{
			Card randomCard2 = GetRandomCard();
			_dealerCards.Add(randomCard2);
			DisplayCard(randomCard2, dealerCardTemplate);
			yield return new WaitForSeconds(1f);
			if (!IsDialogActive())
			{
				yield break;
			}
			score = GetScore(_dealerCards);
			dealerScoreLabel.Arguments = new { score };
		}
		int score2 = GetScore(_playerCards);
		if (score == score2)
		{
			GameManager.ChangeMoneySafe(_initialBet, TransactionInfo);
			_onFinished("dialog_blackjack_tie".Localize());
		}
		else if (score > 21 || score2 > score)
		{
			float num = 1f * _initialBet;
			GameManager.ChangeMoneySafe(num + _initialBet, TransactionInfo);
			_casinoGameController.ShowVictoryAnimation();
			_onFinished("dialog_blackjack_player_wins".Localize(new
			{
				prize = num.ToShortCurrencyFormat()
			}));
		}
		else
		{
			_onFinished("dialog_casino_no_prize_dialog".Localize());
			_casinoGameController.ShowDefeatAnimation();
		}
	}

	private void DisplayCard(Card card, Transform template)
	{
		InstanceBehavior<SfxManager>.Instance.PlayAudio(SoundType.BlackJackCard, _casinoGameController.transform.position);
		Transform transform = UnityEngine.Object.Instantiate(template, template.parent);
		transform.GetComponent<Image>().sprite = card.icon;
		transform.gameObject.SetActive(value: true);
		transform.GetComponent<Image>().DOFade(1f, 1f).SetLink(transform.gameObject);
	}

	private Card GetRandomCard()
	{
		return cardsDeck.FindAll((Card x) => !_dealerCards.Contains(x) && !_playerCards.Contains(x)).GetRandom();
	}

	private int GetScore(List<Card> cards)
	{
		bool flag = false;
		int num = 0;
		foreach (Card card in cards)
		{
			if (CardIsAce(card))
			{
				flag = true;
			}
			num += card.value;
		}
		if ((num <= 11) & flag)
		{
			num += 10;
		}
		return num;
	}

	private static bool CardIsAce(Card card)
	{
		return card.value == 1;
	}

	private void ChangeAllButtons(bool state)
	{
		hitButton.interactable = state;
		standButton.interactable = state;
		insuranceButton.interactable = state;
		surrenderButton.interactable = state;
	}

	private bool IsDialogActive()
	{
		if (_isDialogActive != null)
		{
			return _isDialogActive();
		}
		return true;
	}
}
