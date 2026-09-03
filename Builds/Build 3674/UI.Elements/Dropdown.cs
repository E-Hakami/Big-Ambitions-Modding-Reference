using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Localizor;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UI.Elements;

public class Dropdown : MonoBehaviour
{
	[SerializeField]
	private float maxOptionsHeight;

	[SerializeField]
	private float maxOptionsWidth;

	[Header("References")]
	[SerializeField]
	private CanvasGroup canvasGroup;

	[SerializeField]
	private RectTransform dropdownRect;

	[SerializeField]
	private Button middleBoxButton;

	[SerializeField]
	private RectTransform middleBoxRect;

	[SerializeField]
	private RectTransform headerRect;

	[SerializeField]
	private Button previousOptionButton;

	[SerializeField]
	private Button nextOptionButton;

	[SerializeField]
	private TMP_InputField selectedOptionInputField;

	[SerializeField]
	private TMP_Text selectedOptionPlaceholderLabel;

	[SerializeField]
	private string placeholderOverrideKey;

	[SerializeField]
	private RectTransform optionsPanelParentRect;

	public RectTransform optionsPanelRect;

	[SerializeField]
	private RectTransform viewportRect;

	[SerializeField]
	private RectTransform optionsScrollRect;

	[SerializeField]
	private CanvasGroup optionsCanvasGroup;

	[SerializeField]
	private RectTransform downArrowRect;

	[SerializeField]
	private GameObject inputEnabledVisuals;

	[SerializeField]
	private LayoutElement dropdownLayoutElement;

	[SerializeField]
	private DropdownOptionScrollerController optionScrollerController;

	private static readonly char[] FilterSeparator = new char[1] { ' ' };

	[HideInInspector]
	public UnityEvent<int> onOptionSelected = new UnityEvent<int>();

	[HideInInspector]
	public UnityEvent<string> onSearchTextChanged = new UnityEvent<string>();

	[HideInInspector]
	public UnityEvent onMiddleBoxClicked = new UnityEvent();

	private float _optionsPanelHeightMargin;

	private float _optionHeight;

	private List<string> _options = new List<string>();

	private List<string> _optionSearchTexts = new List<string>();

	private int _selectedOptionIndex = -1;

	private string _placeholderText;

	private bool _filtering;

	private readonly List<int> _filteredOptions = new List<int>();

	private int _controllerHoveredOption = -1;

	private bool _localizeOptions;

	private HoverEffect[] _hoverEffects;

	private bool _isOptionsHeightDirty;

	private float _highestPreferredOptionsWidth;

	public static Dropdown currentDropdown;

	private Vector2 _originalOptionsPanelAnchoredPosition;

	public int SelectedOptionIndex => _selectedOptionIndex;

	public string SelectedOption
	{
		get
		{
			if (_selectedOptionIndex != -1)
			{
				return _options[_selectedOptionIndex];
			}
			return "";
		}
	}

	private float HighestPreferredOptionsWidth => _highestPreferredOptionsWidth + 100f;

	public void SetInteractable(bool interactable)
	{
		canvasGroup.interactable = interactable;
		HoverEffect[] hoverEffects = _hoverEffects;
		foreach (HoverEffect obj in hoverEffects)
		{
			obj.enabled = interactable;
			obj.OnPointerExit(null);
		}
	}

	private void SetUpEvents()
	{
		middleBoxButton.onClick.RemoveAllListeners();
		middleBoxButton.onClick.AddListener(ClickMiddleBox);
		if (selectedOptionInputField != null)
		{
			selectedOptionInputField.onDeselect.RemoveAllListeners();
			selectedOptionInputField.onDeselect.AddListener(delegate
			{
				if (!MouseIsOverDropdown())
				{
					DeselectSearchInput();
					HideOptions();
				}
			});
			selectedOptionInputField.onValueChanged.RemoveAllListeners();
			selectedOptionInputField.onValueChanged.AddListener(UpdateSearchInput);
		}
		previousOptionButton?.onClick.RemoveAllListeners();
		previousOptionButton?.onClick.AddListener(SelectPreviousOption);
		nextOptionButton?.onClick.RemoveAllListeners();
		nextOptionButton?.onClick.AddListener(SelectNextOption);
	}

	public void InitEmpty(string placeholderText, bool localize = true)
	{
		_hoverEffects = headerRect.GetComponentsInChildren<HoverEffect>();
		SetPlaceholder(placeholderText, localize);
		UpdateSelectedOptionText();
	}

	private void Awake()
	{
		if (_placeholderText == null)
		{
			_placeholderText = ResolvePlaceholderText();
		}
		LocalizorManager.OnLanguageChanged = (Action)Delegate.Combine(LocalizorManager.OnLanguageChanged, new Action(ReLocalizeTexts));
		optionsCanvasGroup.alpha = 0f;
	}

	private void OnDestroy()
	{
		LocalizorManager.OnLanguageChanged = (Action)Delegate.Remove(LocalizorManager.OnLanguageChanged, new Action(ReLocalizeTexts));
	}

	private void OnEnable()
	{
		SetPreferredWidth();
	}

	private void ClickMiddleBox()
	{
		if (optionsPanelParentRect.gameObject.activeSelf)
		{
			HideOptions();
		}
		else
		{
			List<string> options = _options;
			if (options != null && options.Count > 0)
			{
				ShowOptions();
			}
		}
		onMiddleBoxClicked.Invoke();
	}

	private void ShowOptions()
	{
		if (base.gameObject.activeInHierarchy)
		{
			StartCoroutine(ShowOptionsCoroutine());
		}
	}

	private IEnumerator ShowOptionsCoroutine()
	{
		if (currentDropdown != null)
		{
			currentDropdown.HideOptions();
		}
		currentDropdown = this;
		optionsPanelParentRect.gameObject.SetActive(value: true);
		if (_isOptionsHeightDirty)
		{
			yield return SetOptionsPanelSize();
		}
		CheckIfShouldShowOptionsAbove();
		if ((bool)downArrowRect)
		{
			downArrowRect.eulerAngles = Vector3.forward * -90f;
		}
		if (Gamepad.current != null && !_filtering)
		{
			_controllerHoveredOption = _selectedOptionIndex;
		}
		else
		{
			ScrollToSelectedOption();
		}
		if ((bool)selectedOptionInputField)
		{
			selectedOptionInputField.Select();
		}
		optionsCanvasGroup.DOKill();
		optionsCanvasGroup.DOFade(1f, 0.2f).SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject);
		selectedOptionPlaceholderLabel.text = _placeholderText;
	}

	private void CheckIfShouldShowOptionsAbove()
	{
		float num = RectTransformUtility.WorldToScreenPoint(null, dropdownRect.position).y - dropdownRect.sizeDelta.y * dropdownRect.pivot.y * dropdownRect.lossyScale.y;
		float y = optionsPanelParentRect.sizeDelta.y;
		if (num < (y + 20f) * dropdownRect.lossyScale.y)
		{
			Vector2 anchoredPosition = new Vector2(optionsPanelParentRect.anchoredPosition.x, y + 10f);
			optionsPanelParentRect.anchoredPosition = anchoredPosition;
		}
		else
		{
			optionsPanelParentRect.anchoredPosition = _originalOptionsPanelAnchoredPosition;
		}
	}

	public void HideOptions()
	{
		optionsCanvasGroup.DOKill();
		optionsCanvasGroup.DOFade(0f, 0.2f).SetUpdate(isIndependentUpdate: true).SetLink(base.gameObject);
		currentDropdown = null;
		optionsPanelParentRect.gameObject.SetActive(value: false);
		if (inputEnabledVisuals != null)
		{
			inputEnabledVisuals.SetActive(value: false);
		}
		if ((bool)downArrowRect)
		{
			downArrowRect.eulerAngles = Vector3.forward * 90f;
		}
		DeselectSearchInput();
	}

	public void ClickAction()
	{
		if (!(this == null) && !(currentDropdown != this))
		{
			if (!optionsPanelRect.gameObject.activeSelf)
			{
				currentDropdown = null;
			}
			else if (!MouseIsOverDropdown())
			{
				HideOptions();
			}
		}
	}

	public void SetOptions(List<string> newOptions, bool localize = true, int selectedOption = -1, List<string> optionSearchTexts = null)
	{
		SetUpEvents();
		if (_hoverEffects == null)
		{
			_hoverEffects = headerRect.GetComponentsInChildren<HoverEffect>();
		}
		_options = newOptions;
		_selectedOptionIndex = selectedOption;
		_localizeOptions = localize;
		_optionSearchTexts = optionSearchTexts;
		_highestPreferredOptionsWidth = 0f;
		if (selectedOptionInputField != null)
		{
			selectedOptionInputField.text = "";
		}
		LoadDropdownOptions();
		UpdateSelectedOptionText();
		_isOptionsHeightDirty = true;
	}

	private void LoadDropdownOptions(bool updateSelected = true)
	{
		List<int> filteredOptions = null;
		if (_filtering)
		{
			filteredOptions = _filteredOptions;
		}
		optionScrollerController.LoadDropdownOptions(_options, filteredOptions, _localizeOptions, _selectedOptionIndex, updateSelected);
		_highestPreferredOptionsWidth = optionScrollerController.GetHighestPreferredOptionsWidth();
	}

	private void UpdateOptions()
	{
		if (_options == null || _options.Count == 0 || (_filtering && _filteredOptions.Count == 0))
		{
			HideOptions();
			return;
		}
		LoadDropdownOptions(updateSelected: false);
		_isOptionsHeightDirty = true;
		ShowOptions();
	}

	private void ReLocalizeTexts()
	{
		_placeholderText = ResolvePlaceholderText();
		UpdateSelectedOptionText();
	}

	public void SetVisualSelectedOption(int optionId)
	{
		if (optionId >= 0 && optionId < _options.Count && _selectedOptionIndex != optionId)
		{
			_selectedOptionIndex = optionId;
			UpdateSelectedOptionText();
		}
	}

	private void UpdateSelectedOptionText()
	{
		if (string.IsNullOrEmpty(_placeholderText))
		{
			_placeholderText = ResolvePlaceholderText();
		}
		selectedOptionPlaceholderLabel.text = ((_selectedOptionIndex == -1) ? _placeholderText : (_localizeOptions ? _options[_selectedOptionIndex].GetLocalization() : _options[_selectedOptionIndex]));
	}

	private string ResolvePlaceholderText()
	{
		string text = (string.IsNullOrEmpty(placeholderOverrideKey) ? "filebrowser_search" : placeholderOverrideKey);
		string localization = text.GetLocalization();
		if (!string.IsNullOrEmpty(localization))
		{
			return localization;
		}
		return text;
	}

	public void SelectOption(string option)
	{
		if (_options.Contains(option))
		{
			SelectOption(_options.IndexOf(option));
		}
	}

	public void SelectOption(DropdownOptionModel option)
	{
		SelectOption(option.optionId);
	}

	public void SelectOption(int optionId)
	{
		bool filtering = _filtering;
		if (_filtering)
		{
			_filtering = false;
			_filteredOptions.Clear();
			if (selectedOptionInputField != null)
			{
				selectedOptionInputField.SetTextWithoutNotify("");
			}
		}
		if (filtering || _selectedOptionIndex != optionId)
		{
			_selectedOptionIndex = optionId;
			SetOptions(_options, _localizeOptions, optionId, _optionSearchTexts);
			onOptionSelected.Invoke(optionId);
			UpdateSelectedOptionText();
		}
		HideOptions();
	}

	public void ResetSelectedOption(int defaultOption = -1)
	{
		SetOptions(_options, _localizeOptions, defaultOption, _optionSearchTexts);
	}

	public void SelectNextOption()
	{
		if (_selectedOptionIndex >= _options.Count - 1)
		{
			SelectOption(0);
		}
		else
		{
			SelectOption(_selectedOptionIndex + 1);
		}
	}

	public void SelectPreviousOption()
	{
		if (_selectedOptionIndex <= 0)
		{
			SelectOption(_options.Count - 1);
		}
		else
		{
			SelectOption(_selectedOptionIndex - 1);
		}
	}

	private IEnumerator SetOptionsPanelSize()
	{
		if (base.gameObject.activeSelf && _options != null)
		{
			if ((_filtering ? _filteredOptions.Count : _options.Count) == 0)
			{
				yield return 0;
			}
			yield return null;
			float num = optionScrollerController.GetTotalOptionsHeight() + Mathf.Abs(viewportRect.offsetMax.y) + MathF.Abs(viewportRect.offsetMin.y);
			if (num > maxOptionsHeight)
			{
				num = maxOptionsHeight;
			}
			float num2 = dropdownRect.rect.width;
			float highestPreferredOptionsWidth = HighestPreferredOptionsWidth;
			if (num2 < highestPreferredOptionsWidth)
			{
				num2 = highestPreferredOptionsWidth;
			}
			if (num2 > Mathf.Max(maxOptionsWidth, maxOptionsWidth))
			{
				num2 = maxOptionsWidth;
			}
			optionsPanelParentRect.offsetMax = new Vector2(optionsPanelParentRect.offsetMax.x, 0f - (dropdownRect.rect.height + 5f));
			optionsPanelParentRect.sizeDelta = new Vector2(num2, num);
			_originalOptionsPanelAnchoredPosition = optionsPanelParentRect.anchoredPosition;
			SetPreferredWidth();
		}
	}

	private void SetPreferredWidth()
	{
		float highestPreferredOptionsWidth = HighestPreferredOptionsWidth;
		dropdownLayoutElement.preferredWidth = highestPreferredOptionsWidth + middleBoxRect.offsetMin.magnitude + middleBoxRect.offsetMax.magnitude;
	}

	private void ScrollToSelectedOption()
	{
		optionScrollerController.ScrollToSelectedOption(_selectedOptionIndex);
	}

	public void SetPlaceholder(string text, bool localize = true)
	{
		_placeholderText = (localize ? text.GetLocalization() : text);
	}

	private void SelectSearchInput()
	{
		if (_options.Count != 0)
		{
			ShowOptions();
			if (inputEnabledVisuals != null)
			{
				inputEnabledVisuals.SetActive(value: true);
			}
			selectedOptionPlaceholderLabel.text = "";
		}
	}

	public void UpdateSearchInput(string filter)
	{
		onSearchTextChanged.Invoke(filter);
		if (_options.Count == 0)
		{
			return;
		}
		filter = filter.ToLower();
		_filteredOptions.Clear();
		if (string.IsNullOrEmpty(filter))
		{
			_filtering = false;
		}
		else
		{
			_filtering = true;
			for (int i = 0; i < _options.Count; i++)
			{
				if (IsFilterOrItsWordsPartOfOption(option: ((_optionSearchTexts == null || i >= _optionSearchTexts.Count) ? (_localizeOptions ? _options[i].GetLocalization() : _options[i]) : _optionSearchTexts[i]).ToLower(), filter: filter.ToLower()))
				{
					_filteredOptions.Add(i);
				}
			}
		}
		UpdateOptions();
	}

	private bool IsFilterOrItsWordsPartOfOption(string filter, string option)
	{
		if (option.IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)
		{
			return true;
		}
		string[] array = filter.Split(FilterSeparator, StringSplitOptions.RemoveEmptyEntries);
		foreach (string value in array)
		{
			if (option.IndexOf(value, StringComparison.OrdinalIgnoreCase) < 0)
			{
				return false;
			}
		}
		return true;
	}

	private void DeselectSearchInput()
	{
		if (_options == null || _options.Count == 0)
		{
			if (inputEnabledVisuals != null)
			{
				inputEnabledVisuals.SetActive(value: false);
			}
			UpdateSelectedOptionText();
			return;
		}
		if (inputEnabledVisuals != null)
		{
			inputEnabledVisuals.SetActive(value: false);
		}
		if (_filtering)
		{
			if (string.IsNullOrEmpty(selectedOptionInputField.text))
			{
				_filtering = false;
				_filteredOptions.Clear();
			}
			else if (_options.Contains(selectedOptionInputField.text))
			{
				SelectOption(_options.IndexOf(selectedOptionInputField.text));
				selectedOptionInputField.text = "";
				_filtering = false;
				_filteredOptions.Clear();
				UpdateOptions();
			}
		}
		UpdateSelectedOptionText();
	}

	public void ResetSearch()
	{
		selectedOptionInputField.text = "";
		HideOptions();
	}

	private bool MouseIsOverDropdown()
	{
		if (!Input.mousePresent)
		{
			return false;
		}
		Vector3 point = optionsPanelRect.InverseTransformPoint(Input.mousePosition);
		if (optionsPanelRect.rect.Contains(point))
		{
			return true;
		}
		point = headerRect.InverseTransformPoint(Input.mousePosition);
		if (headerRect.rect.Contains(point))
		{
			return true;
		}
		return false;
	}
}
