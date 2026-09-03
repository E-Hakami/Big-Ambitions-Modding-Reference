using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters.Skills;
using Buildings.Office.Headquarters;
using DG.Tweening;
using Extensions;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

public class HeadhuntersRecruitingTab : MonoBehaviour
{
	private const float SliderFillAnimationDuration = 0.8f;

	private const float BubblesOffset = 48f;

	[SerializeField]
	private HeadhunterPlanUI planUI;

	[Header("Start Recruiting")]
	[SerializeField]
	private GameObject employeeTypeSelectorPanel;

	[SerializeField]
	private UI.Elements.Dropdown skillNameDropdown;

	[SerializeField]
	private GameObject startRecruitingButton;

	[SerializeField]
	private GameObject wageAndSkillPanel;

	[SerializeField]
	private Slider skillTargetSlider;

	[SerializeField]
	private TMP_Text sliderSkillLabel;

	[SerializeField]
	private TextLocalizationComponent wageRangeLabel;

	[SerializeField]
	private HeadhuntersDealBreakers dealBreakers;

	[SerializeField]
	private GameObject clearAllDealBreakersButton;

	[SerializeField]
	private GameObject pointsSliderPanel;

	[SerializeField]
	private RectTransform pointsSliderBackground;

	[SerializeField]
	private Image pointsUsedSliderImage;

	[SerializeField]
	private RectTransform pointsUsedBubble;

	[SerializeField]
	private TextLocalizationComponent pointsUsedBubbleLabel;

	[SerializeField]
	private Image headhunterSkillSliderImage;

	[SerializeField]
	private RectTransform headhunterSkillBubble;

	[SerializeField]
	private TextLocalizationComponent headhunterSkillBubbleLabel;

	[Header("Recruiting")]
	[SerializeField]
	private GameObject viewCurrentRecruitsButton;

	[SerializeField]
	private GameObject stopRecruitingButton;

	[SerializeField]
	private GameObject currentlyRecruitingPanel;

	[SerializeField]
	private GameObject recruitmentSettingsPanel;

	[SerializeField]
	private TextLocalizationComponent employeeSkillNameLabel;

	[SerializeField]
	private TextLocalizationComponent skillLevelLabel;

	[SerializeField]
	private TextLocalizationComponent salaryRangeLabel;

	[SerializeField]
	private TMP_InputField amountOfCandidatesField;

	[SerializeField]
	private Toggle recruitContinuousToggle;

	[SerializeField]
	private Toggle recruitAmountOfCandidatesToggle;

	private List<string> _skillsToRecruit;

	private void Awake()
	{
		SetUpSkillsToRecruitArray();
		skillTargetSlider.onValueChanged.AddListener(SetSkillTargetValue);
		amountOfCandidatesField.onEndEdit.AddListener(OnAmountOfCandidatesSet);
		HeadhuntersDealBreakers headhuntersDealBreakers = dealBreakers;
		headhuntersDealBreakers.onDealBreakerToggled = (Action<string, bool>)Delegate.Combine(headhuntersDealBreakers.onDealBreakerToggled, new Action<string, bool>(ToggleDealBreaker));
	}

	private void Start()
	{
		recruitContinuousToggle.onValueChanged.AddListener(OnRecruitContinuouslyToggle);
		recruitAmountOfCandidatesToggle.onValueChanged.AddListener(OnRecruitAmountOfCandidatesToggle);
	}

	private void OnEnable()
	{
		SetUpRecruitingInfo();
	}

	private void OnDisable()
	{
		StopAllCoroutines();
		ResetFillingAnimations();
	}

	private void SetUpRecruitingInfo()
	{
		if (planUI.currentPlan.isRecruiting)
		{
			ShowRecruitingInfo();
		}
		else
		{
			ShowStartRecruitingInfo();
		}
	}

	public void StartRecruiting()
	{
		planUI.currentPlan.StartRecruiting();
		ShowRecruitingInfo();
	}

	public void StopRecruiting()
	{
		planUI.currentPlan.isRecruiting = false;
		ShowStartRecruitingInfo();
	}

	public void ShowCurrentRecruits()
	{
		InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.initialTab = "Candidates";
		InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.MyEmployees);
	}

	private void SetSkillTargetValue(float value)
	{
		planUI.currentPlan.skillValueTarget = value;
		sliderSkillLabel.SetText($"{Mathf.RoundToInt(value)}%");
		UpdateWageRangeLabel();
	}

	public void ClearAllDealBreakers()
	{
		planUI.currentPlan.dealBreakerTypes.Clear();
		dealBreakers.toggledDealBreakersTypes.Clear();
		dealBreakers.SetDealBreakersForSkill(planUI.currentPlan.skillRecruiting, enableInteraction: true);
		SetPointsUsed(dealBreakers.usedDealBreakersPoints);
	}

	private void ShowRecruitingInfo()
	{
		employeeTypeSelectorPanel.SetActive(value: false);
		wageAndSkillPanel.SetActive(value: false);
		clearAllDealBreakersButton.SetActive(value: false);
		pointsSliderPanel.SetActive(value: false);
		startRecruitingButton.SetActive(value: false);
		SetUpCurrentlyRecruitingInfo();
		dealBreakers.toggledDealBreakersTypes = planUI.currentPlan.dealBreakerTypes.ToList();
		dealBreakers.SetDealBreakersForSkill(planUI.currentPlan.skillRecruiting, enableInteraction: false);
		stopRecruitingButton.SetActive(value: true);
		viewCurrentRecruitsButton.SetActive(value: true);
		recruitmentSettingsPanel.SetActive(value: true);
		SetUpRecruitmentSettingsPanel();
	}

	private void SetUpCurrentlyRecruitingInfo()
	{
		employeeSkillNameLabel.Key = planUI.currentPlan.skillRecruiting;
		skillLevelLabel.Arguments = new
		{
			minSkill = planUI.currentPlan.MinSkillTarget,
			maxSkill = planUI.currentPlan.MaxSkillTarget
		};
		var (val, val2) = planUI.currentPlan.GetWageRangeForSkill(planUI.currentPlan.skillRecruiting);
		salaryRangeLabel.Arguments = new
		{
			minWagePerHour = val.ToShortCurrencyFormat(),
			maxWagePerHour = val2.ToShortCurrencyFormat()
		};
		currentlyRecruitingPanel.SetActive(value: true);
	}

	public void OnRecruitContinuouslyToggle(bool isOn)
	{
		if (isOn && planUI.currentPlan.remainingCandidatesToRecruit != -1)
		{
			planUI.currentPlan.remainingCandidatesToRecruit = -1;
			planUI.currentPlan.amountOfCandidatesToRecruitPreference = -1;
			amountOfCandidatesField.gameObject.SetActive(value: false);
		}
	}

	public void OnRecruitAmountOfCandidatesToggle(bool isOn)
	{
		if (isOn && planUI.currentPlan.remainingCandidatesToRecruit == -1)
		{
			planUI.currentPlan.remainingCandidatesToRecruit = 10;
			planUI.currentPlan.amountOfCandidatesToRecruitPreference = 10;
			amountOfCandidatesField.SetTextWithoutNotify(10.ToString());
			amountOfCandidatesField.gameObject.SetActive(value: true);
		}
	}

	private void OnAmountOfCandidatesSet(string newAmountOfCandidates)
	{
		int num = ((string.IsNullOrEmpty(newAmountOfCandidates) || !int.TryParse(newAmountOfCandidates, out var result)) ? 1 : Math.Clamp(result, 1, 999));
		planUI.currentPlan.remainingCandidatesToRecruit = num;
		planUI.currentPlan.amountOfCandidatesToRecruitPreference = num;
		amountOfCandidatesField.SetTextWithoutNotify(num.ToString());
	}

	private void SetUpRecruitmentSettingsPanel()
	{
		int remainingCandidatesToRecruit = planUI.currentPlan.remainingCandidatesToRecruit;
		if (remainingCandidatesToRecruit == -1)
		{
			recruitContinuousToggle.SetIsOnWithoutNotify(value: true);
			recruitAmountOfCandidatesToggle.SetIsOnWithoutNotify(value: false);
			amountOfCandidatesField.gameObject.SetActive(value: false);
		}
		else
		{
			recruitContinuousToggle.SetIsOnWithoutNotify(value: false);
			recruitAmountOfCandidatesToggle.SetIsOnWithoutNotify(value: true);
			amountOfCandidatesField.SetTextWithoutNotify(remainingCandidatesToRecruit.ToString());
			amountOfCandidatesField.gameObject.SetActive(value: true);
		}
	}

	private void ShowStartRecruitingInfo()
	{
		stopRecruitingButton.SetActive(value: false);
		viewCurrentRecruitsButton.SetActive(value: false);
		currentlyRecruitingPanel.SetActive(value: false);
		recruitmentSettingsPanel.SetActive(value: false);
		ResetFillingAnimations();
		SetUpEmployeeTypeSelectorPanel();
		SetUpWageAndSkillPanel();
		dealBreakers.availableDealBreakersPoints = planUI.currentPlan.AvailableDealBreakersPoints;
		dealBreakers.toggledDealBreakersTypes = planUI.currentPlan.dealBreakerTypes.ToList();
		dealBreakers.SetDealBreakersForSkill(planUI.currentPlan.skillRecruiting, enableInteraction: true);
		SetUpPointsSliderPanel();
		clearAllDealBreakersButton.SetActive(value: true);
		startRecruitingButton.SetActive(value: true);
	}

	private void SetUpEmployeeTypeSelectorPanel()
	{
		skillNameDropdown.SetOptions(_skillsToRecruit.ToList(), localize: true, _skillsToRecruit.IndexOf(planUI.currentPlan.skillRecruiting));
		skillNameDropdown.onOptionSelected.RemoveAllListeners();
		skillNameDropdown.onOptionSelected.AddListener(SelectSkillToRecruit);
		employeeTypeSelectorPanel.SetActive(value: true);
	}

	private void SetUpWageAndSkillPanel()
	{
		skillTargetSlider.minValue = 10f;
		skillTargetSlider.value = planUI.currentPlan.skillValueTarget;
		UpdateWageRangeLabel();
		wageAndSkillPanel.SetActive(value: true);
	}

	private void SetUpPointsSliderPanel()
	{
		SetPointsUsed(dealBreakers.usedDealBreakersPoints);
		SetHeadhunterSkill(planUI.currentPlan.HeadhunterSkillValue);
		pointsSliderPanel.SetActive(value: true);
	}

	private void SelectSkillToRecruit(int selectedSkillIndex)
	{
		string text = _skillsToRecruit[selectedSkillIndex];
		string[][] dealBreakersForSkill = HeadhunterHelper.GetDealBreakersForSkill(planUI.currentPlan.skillRecruiting);
		string[][] dealBreakersForSkill2 = HeadhunterHelper.GetDealBreakersForSkill(text);
		if (dealBreakersForSkill != dealBreakersForSkill2)
		{
			planUI.currentPlan.dealBreakerTypes.Clear();
			dealBreakers.toggledDealBreakersTypes.Clear();
		}
		planUI.currentPlan.skillRecruiting = text;
		UpdateWageRangeLabel();
		dealBreakers.SetDealBreakersForSkill(planUI.currentPlan.skillRecruiting, enableInteraction: true);
		SetPointsUsed(dealBreakers.usedDealBreakersPoints);
	}

	private void UpdateWageRangeLabel()
	{
		var (val, val2) = planUI.currentPlan.GetWageRangeForSkill(planUI.currentPlan.skillRecruiting);
		wageRangeLabel.Arguments = new
		{
			minWagePerHour = val.ToShortCurrencyFormat(),
			maxWagePerHour = val2.ToShortCurrencyFormat()
		};
	}

	private void ToggleDealBreaker(string dealBreakerType, bool toggled)
	{
		if (toggled)
		{
			planUI.currentPlan.dealBreakerTypes.Add(dealBreakerType);
		}
		else
		{
			planUI.currentPlan.dealBreakerTypes.Remove(dealBreakerType);
		}
		SetPointsUsed(dealBreakers.usedDealBreakersPoints);
	}

	private void SetPointsUsed(int pointsUsed)
	{
		float fillAmount = (float)pointsUsed / 100f;
		pointsUsedBubbleLabel.Arguments = new { pointsUsed };
		StartCoroutine(FillSliderAndBubble(fillAmount, pointsUsedSliderImage, pointsUsedBubble));
	}

	private void SetHeadhunterSkill(float skillValue)
	{
		float fillAmount = skillValue / 100f;
		headhunterSkillBubbleLabel.Arguments = new
		{
			skillPercentage = Mathf.FloorToInt(skillValue)
		};
		StartCoroutine(FillSliderAndBubble(fillAmount, headhunterSkillSliderImage, headhunterSkillBubble));
	}

	private IEnumerator FillSliderAndBubble(float fillAmount, Image slider, RectTransform bubble)
	{
		yield return new WaitForSecondsRealtime(0.1f);
		float endValue = pointsSliderBackground.rect.width * fillAmount + 48f;
		bubble.DOKill();
		bubble.DOAnchorPosX(endValue, 0.8f).SetUpdate(isIndependentUpdate: true);
		slider.DOKill();
		slider.DOFillAmount(fillAmount, 0.8f).SetUpdate(isIndependentUpdate: true);
	}

	private void SetUpSkillsToRecruitArray()
	{
		List<string> list = new List<string>();
		foreach (string allSkillName in SkillHelper.AllSkillNames)
		{
			if (!HeadhunterHelper.skillsToSkipWhenRecruiting.Contains(allSkillName))
			{
				list.Add(allSkillName);
			}
		}
		_skillsToRecruit = list;
	}

	private void ResetFillingAnimations()
	{
		pointsUsedSliderImage.DOKill();
		pointsUsedSliderImage.fillAmount = 0f;
		pointsUsedBubble.DOKill();
		pointsUsedBubble.anchoredPosition = new Vector2(48f, pointsUsedBubble.anchoredPosition.y);
		headhunterSkillSliderImage.DOKill();
		headhunterSkillSliderImage.fillAmount = 0f;
		headhunterSkillBubble.DOKill();
		headhunterSkillBubble.anchoredPosition = new Vector2(48f, headhunterSkillBubble.anchoredPosition.y);
	}
}
