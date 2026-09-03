using System;
using System.Collections;
using System.Collections.Generic;
using BigAmbitions.DayNightCycle;
using Character.Customization;
using DG.Tweening;
using Entities;
using Extensions;
using Helpers;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Load;
using UI.Topbar.Accessories;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Topbar;

public class Topbar : MonoBehaviour
{
	private class MoneyTransaction
	{
		public float amount;

		public Timestamp timestamp;
	}

	[Header("Stats bars")]
	[SerializeField]
	private Image energyFiller;

	[SerializeField]
	private Image hungerFiller;

	[SerializeField]
	private Image happinessFiller;

	[Space]
	public TextLocalizationComponent dateLabel;

	public TextMeshProUGUI timeLabel;

	public TextMeshProUGUI money;

	public TextMeshProUGUI moneyChange;

	private Transform _moneyChangePositive;

	private Transform _moneyChangeNegative;

	public TextMeshProUGUI moneyTransactionTemplate;

	private readonly List<MoneyTransaction> _transactionQueue = new List<MoneyTransaction>();

	private Coroutine _activeMoneyAnimation;

	public GameObject container;

	public TextMeshProUGUI fullMenuMoney;

	public TextMeshProUGUI fullMenuMoneyChange;

	private Transform _fullMenuMoneyChangePositive;

	private Transform _fullMenuMoneyChangeNegative;

	public Image avatar;

	public PlayerDancesUI playerDancesUI;

	public AccessoriesUI accessoriesUI;

	[SerializeField]
	private BasicTooltip moneyTooltip;

	[SerializeField]
	private BasicTooltip yearsTooltip;

	private FinancialSummary _yesterdaysSum;

	private int _lastDayUpdated = -1;

	private int _lastMinuteUpdated = -1;

	private float _lastMoneyUpdated = -1f;

	private readonly WaitForSecondsRealtime _wfsr = new WaitForSecondsRealtime(0.4f);

	private const float Duration = 4f;

	private void OnEnable()
	{
		LocalizorManager.OnLanguageChanged = (Action)Delegate.Combine(LocalizorManager.OnLanguageChanged, new Action(InvalidateTimeLabel));
		InvalidateTimeLabel();
	}

	private void OnDisable()
	{
		LocalizorManager.OnLanguageChanged = (Action)Delegate.Remove(LocalizorManager.OnLanguageChanged, new Action(InvalidateTimeLabel));
	}

	private void Start()
	{
		container.gameObject.SetActive(value: true);
		accessoriesUI.UpdateUI(SaveGameManager.Current.accessoriesData);
		if ((bool)moneyChange)
		{
			_moneyChangeNegative = moneyChange.transform.Find("NegativeArrow");
			_moneyChangePositive = moneyChange.transform.Find("PositiveArrow");
		}
		if ((bool)fullMenuMoneyChange)
		{
			_fullMenuMoneyChangeNegative = fullMenuMoneyChange.transform.Find("NegativeArrow");
			_fullMenuMoneyChangePositive = fullMenuMoneyChange.transform.Find("PositiveArrow");
		}
		SetMoneyChangeValue();
		SetYearsValue();
		GlobalEvents.onNewDay = (Action)Delegate.Combine(GlobalEvents.onNewDay, new Action(SetMoneyChangeValue));
		GlobalEvents.onNewDay = (Action)Delegate.Combine(GlobalEvents.onNewDay, new Action(SetYearsValue));
		GlobalEvents.onCultureInfoChanged = (Action)Delegate.Combine(GlobalEvents.onCultureInfoChanged, new Action(SetMoneyChangeValue));
		GlobalEvents.onFullMenuToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onFullMenuToggle, (Action<bool>)delegate(bool show)
		{
			base.gameObject.SetActive(!show);
		});
		GlobalEvents.RegisterOnGameLoadedCallback(SetMoneyChangeValue);
		GlobalEvents.RegisterOnGameLoadedCallback(LoadPortrait);
	}

	private void Update()
	{
		if (LoadScene.isLoading)
		{
			return;
		}
		UpdateStatsBars();
		if (_lastDayUpdated != SaveGameManager.Current.Day)
		{
			dateLabel.Arguments = new
			{
				DayOfWeek = "common_" + TimeHelper.GetDayOfWeek(),
				CurrentNumberDay = SaveGameManager.Current.Day
			};
			_lastDayUpdated = SaveGameManager.Current.Day;
		}
		int num = SaveGameManager.Current.Hour * 60 + (int)SaveGameManager.Current.Minute;
		if (_lastMinuteUpdated != num)
		{
			timeLabel.SetCurrentFormattedTime();
			_lastMinuteUpdated = num;
		}
		UpdateMoneyValue();
		if (_activeMoneyAnimation != null)
		{
			return;
		}
		MoneyTransaction moneyTransaction = ((_transactionQueue.Count > 0) ? _transactionQueue[0] : null);
		_transactionQueue.Remove(moneyTransaction);
		float sign;
		float time;
		if (moneyTransaction != null)
		{
			if (_transactionQueue.Count != 0)
			{
				sign = Mathf.Sign(moneyTransaction.amount);
				time = moneyTransaction.timestamp.GetTotalMinutes();
				float transactionSum = GetTransactionSum(Combine);
				_transactionQueue.RemoveAll(Combine);
				moneyTransaction.amount += transactionSum;
			}
			_activeMoneyAnimation = StartCoroutine(ShowMoneyTransaction(moneyTransaction.amount));
		}
		bool Combine(MoneyTransaction x)
		{
			if ((int)x.timestamp.GetTotalMinutes() == (int)time)
			{
				return Mathf.Sign(x.amount) == sign;
			}
			return false;
		}
	}

	private float GetTransactionSum(Predicate<MoneyTransaction> match)
	{
		float num = 0f;
		foreach (MoneyTransaction item in _transactionQueue)
		{
			if (match(item))
			{
				num += item.amount;
			}
		}
		return num;
	}

	private void LoadPortrait()
	{
		Sprite sprite = PortraitGenerator.LoadPlayerPortrait();
		if (sprite != null)
		{
			InstanceBehavior<UIs>.Instance.topBar.avatar.sprite = sprite;
		}
		else
		{
			PortraitGenerator.Create(SaveGameManager.Current.charactersData[0], PortraitGenerator.GetCharacterPortraitPath(SaveGameManager.Current), InstanceBehavior<UIs>.Instance.topBar.avatar);
		}
	}

	private void InvalidateTimeLabel()
	{
		_lastMinuteUpdated = -1;
	}

	private void UpdateStatsBars()
	{
		float num = SaveGameManager.Current.Energy / 100f;
		if ((num == 0f && energyFiller.fillAmount != 0f) || Mathf.Abs(energyFiller.fillAmount - num) > 0.005f)
		{
			energyFiller.fillAmount = num;
		}
		float num2 = SaveGameManager.Current.Hunger / 100f;
		if ((num2 == 0f && hungerFiller.fillAmount != 0f) || Mathf.Abs(hungerFiller.fillAmount - num2) > 0.01f)
		{
			hungerFiller.fillAmount = num2;
		}
		float num3 = HappinessHelper.Happiness / 100f;
		if (Mathf.Abs(happinessFiller.fillAmount - num3) > 0.01f)
		{
			happinessFiller.fillAmount = num3;
		}
	}

	public bool UpdateMoneyValue()
	{
		if (Math.Abs(_lastMoneyUpdated - SaveGameManager.Current.Money) > 0.1f)
		{
			string text = SaveGameManager.Current.Money.ToShortCurrencyFormat(abbreviated: true);
			if ((bool)money)
			{
				money.text = text;
			}
			if ((bool)fullMenuMoney)
			{
				fullMenuMoney.text = text;
			}
			UpdateMoneyTooltip(moneyTooltip);
			_lastMoneyUpdated = SaveGameManager.Current.Money;
			return true;
		}
		return false;
	}

	private void SetYearsValue()
	{
		int yearsByDays = TimeHelper.GetYearsByDays(SaveGameManager.Current.Day);
		int daysAmount = SaveGameManager.Current.Day - TimeHelper.GetDaysByYears(yearsByDays);
		yearsTooltip.localizationArguments = new
		{
			yearsAmount = yearsByDays,
			daysAmount = daysAmount
		};
	}

	public void AddMoneyTransaction(float amount)
	{
		_transactionQueue.Add(new MoneyTransaction
		{
			amount = amount,
			timestamp = TimeHelper.Now()
		});
	}

	private IEnumerator ShowMoneyTransaction(float amount)
	{
		TextMeshProUGUI newTransaction = UnityEngine.Object.Instantiate(moneyTransactionTemplate, moneyTransactionTemplate.transform.parent);
		newTransaction.text = amount.ToShortCurrencyFormat();
		if (amount > 0f)
		{
			newTransaction.color = InstanceBehavior<GlobalReferences>.Instance.colors.green;
		}
		else
		{
			newTransaction.text = "-" + Mathf.Abs(amount).ToShortCurrencyFormat();
			newTransaction.color = InstanceBehavior<GlobalReferences>.Instance.colors.red;
		}
		newTransaction.gameObject.SetActive(value: true);
		newTransaction.transform.DOLocalMoveY(-400f, 4f).SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject);
		newTransaction.DOFade(0f, 4f).SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject);
		yield return _wfsr;
		newTransaction.gameObject.SetActive(value: true);
		_activeMoneyAnimation = null;
	}

	private void SetMoneyChangeValue()
	{
		_yesterdaysSum = SaveGameManager.Current.financialSummaries.Find((FinancialSummary x) => x.dayNumber == SaveGameManager.Current.Day - 1);
		if (_yesterdaysSum != null)
		{
			float totalProfit = _yesterdaysSum.totalProfit;
			moneyChange.text = totalProfit.ToShortCurrencyFormat(abbreviated: true);
			_moneyChangeNegative.gameObject.SetActive(totalProfit < 0f);
			_moneyChangePositive.gameObject.SetActive(totalProfit >= 0f);
			moneyChange.color = ((totalProfit >= 0f) ? InstanceBehavior<GlobalReferences>.Instance.colors.lime : InstanceBehavior<GlobalReferences>.Instance.colors.lightRed);
			moneyChange.gameObject.SetActive(value: true);
			fullMenuMoneyChange.text = totalProfit.ToShortCurrencyFormat(abbreviated: true);
			_fullMenuMoneyChangeNegative.gameObject.SetActive(totalProfit < 0f);
			_fullMenuMoneyChangePositive.gameObject.SetActive(totalProfit >= 0f);
			fullMenuMoneyChange.color = ((totalProfit >= 0f) ? InstanceBehavior<GlobalReferences>.Instance.colors.lime : InstanceBehavior<GlobalReferences>.Instance.colors.lightRed);
			fullMenuMoneyChange.gameObject.SetActive(value: true);
		}
		else
		{
			moneyChange?.gameObject.SetActive(value: false);
			fullMenuMoneyChange?.gameObject.SetActive(value: false);
		}
		UpdateMoneyTooltip(moneyTooltip);
	}

	public void UpdateMoneyTooltip(TooltipTarget tooltip)
	{
		tooltip.localizationArguments = new
		{
			totalValue = SaveGameManager.Current.Money.ToShortCurrencyFormat(),
			yesterdayValue = ((_yesterdaysSum == null) ? 0.ToShortCurrencyFormat() : _yesterdaysSum.totalProfit.ToShortCurrencyFormat())
		};
	}

	public void ClickMoney()
	{
		InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.EconoView);
	}

	public void ClickPersona()
	{
		InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.Persona);
	}

	public void OpenMiniMenu()
	{
		InstanceBehavior<UIs>.Instance.miniMenuUI.Toggle(show: true);
	}
}
