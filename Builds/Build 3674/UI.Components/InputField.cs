using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Extensions;
using Localizor.LanguageChangeEvent;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Components;

public class InputField : MonoBehaviour
{
	private const char TabCharacter = '\t';

	public TMP_InputField tmpInputField;

	[SerializeField]
	private FormattingType formattingType;

	[ShowIf("formattingType", FormattingType.Number)]
	public bool allowDecimalNumbers = true;

	[ShowIf("formattingType", FormattingType.Number)]
	public long maxNumeralAmount;

	[ShowIf("formattingType", FormattingType.Number)]
	public float maxFloatNumeralAmount;

	[ShowIf("formattingType", FormattingType.Number)]
	public bool allowNegativeValues;

	[Header("Optional")]
	[SerializeField]
	private bool showIcon = true;

	[SerializeField]
	[ShowIf("showIcon")]
	private GameObject defaultIcon;

	[SerializeField]
	[ShowIf("showIcon")]
	private GameObject numberIcon;

	[SerializeField]
	private bool showClearIcon;

	[SerializeField]
	[ShowIf("showClearIcon")]
	private Button clearIcon;

	[SerializeField]
	private bool setPlaceholderText = true;

	[SerializeField]
	[ShowIf("setPlaceholderText")]
	private TextLocalizationComponent placeholder;

	[SerializeField]
	[ShowIf("setPlaceholderText")]
	private string placeholderLocalizeKeyOverride;

	[SerializeField]
	[ShowIf("formattingType", FormattingType.Number)]
	private bool showCurrencySign;

	[SerializeField]
	private TMP_Text autocompleteField;

	public Action<string> onAutoCompleteConfirm;

	private bool _isInitialized;

	private bool _isReplacingSelection;

	private int _lastCaretPosition;

	private string _lastFormattedValue = "";

	private static NumberFormatInfo NumberFormat => CultureHelper.CultureInfo.NumberFormat;

	private void Start()
	{
		if (!_isInitialized)
		{
			Init();
		}
	}

	private void Update()
	{
		if (!tmpInputField.isFocused)
		{
			return;
		}
		if (autocompleteField != null && Input.GetKeyDown(KeyCode.Tab) && !string.IsNullOrEmpty(autocompleteField.text))
		{
			ConfirmAutocomplete();
			return;
		}
		string currencySymbol = NumberFormat.CurrencySymbol;
		int length = currencySymbol.Length;
		if (showCurrencySign && tmpInputField.text.StartsWith(currencySymbol, StringComparison.Ordinal) && tmpInputField.caretPosition < length)
		{
			tmpInputField.caretPosition = length;
		}
		_lastCaretPosition = tmpInputField.caretPosition;
	}

	private void OnEnable()
	{
		TMP_InputField tMP_InputField = tmpInputField;
		tMP_InputField.onValidateInput = (TMP_InputField.OnValidateInput)Delegate.Combine(tMP_InputField.onValidateInput, new TMP_InputField.OnValidateInput(HandleInputValidation));
	}

	private void OnDisable()
	{
		onAutoCompleteConfirm = null;
		TMP_InputField tMP_InputField = tmpInputField;
		tMP_InputField.onValidateInput = (TMP_InputField.OnValidateInput)Delegate.Remove(tMP_InputField.onValidateInput, new TMP_InputField.OnValidateInput(HandleInputValidation));
		if (autocompleteField != null)
		{
			autocompleteField.text = string.Empty;
		}
	}

	private void Init()
	{
		_isInitialized = true;
		if (numberIcon != null)
		{
			numberIcon.SetActive(value: false);
		}
		if (defaultIcon != null)
		{
			defaultIcon.SetActive(value: false);
		}
		if (showClearIcon)
		{
			clearIcon.onClick.AddListener(ClearText);
			tmpInputField.onValueChanged.AddListener(delegate(string val)
			{
				clearIcon.gameObject.SetActive(!string.IsNullOrEmpty(val));
			});
		}
		switch (formattingType)
		{
		case FormattingType.Number:
			tmpInputField.onValueChanged.AddListener(NumeralFormatter);
			tmpInputField.onEndEdit.AddListener(NumeralEndEditFormatter);
			tmpInputField.onValueChanged.Invoke(GetRawValue());
			tmpInputField.contentType = TMP_InputField.ContentType.Standard;
			if (showIcon && (bool)numberIcon)
			{
				numberIcon.SetActive(value: true);
			}
			if (setPlaceholderText)
			{
				placeholder.Key = "common_amount";
			}
			break;
		case FormattingType.Default:
			if (showIcon && (bool)defaultIcon)
			{
				defaultIcon.SetActive(value: true);
			}
			if (setPlaceholderText)
			{
				placeholder.Key = "common_name";
			}
			break;
		}
		if (autocompleteField != null)
		{
			tmpInputField.onValueChanged.AddListener(delegate(string val)
			{
				if (string.IsNullOrEmpty(val) && !string.IsNullOrEmpty(autocompleteField.text))
				{
					autocompleteField.text = string.Empty;
				}
			});
		}
		if (setPlaceholderText && !string.IsNullOrEmpty(placeholderLocalizeKeyOverride))
		{
			placeholder.Key = placeholderLocalizeKeyOverride;
		}
	}

	private char HandleInputValidation(string text, int charIndex, char addedChar)
	{
		if (formattingType == FormattingType.Number)
		{
			_isReplacingSelection = text.Length != tmpInputField.text.Length;
		}
		if (autocompleteField == null || addedChar != '\t' || string.IsNullOrEmpty(autocompleteField.text))
		{
			return addedChar;
		}
		ConfirmAutocomplete();
		return '\0';
	}

	private void ConfirmAutocomplete()
	{
		string text = autocompleteField.text;
		onAutoCompleteConfirm?.Invoke(text);
		tmpInputField.text = text;
		tmpInputField.caretPosition = text.Length;
		autocompleteField.text = string.Empty;
	}

	public void SetText(string value, bool notify = true)
	{
		if (!_isInitialized)
		{
			Init();
		}
		if (notify)
		{
			tmpInputField.text = value;
		}
		else
		{
			tmpInputField.SetTextWithoutNotify(value);
		}
		_lastFormattedValue = value;
	}

	public void SetAutocompleteValue(string autocompleteText)
	{
		if (autocompleteField == null)
		{
			Debug.LogWarning("Autocomplete field is not set. " + autocompleteText + " will not be set.");
			return;
		}
		string text = tmpInputField.text;
		if (string.IsNullOrEmpty(text))
		{
			autocompleteField.text = string.Empty;
			return;
		}
		string text2;
		if (autocompleteText.Length < text.Length)
		{
			text2 = string.Empty;
		}
		else
		{
			string text3 = autocompleteText;
			int length = text.Length;
			text2 = text + text3.Substring(length, text3.Length - length);
		}
		autocompleteText = text2;
		autocompleteField.text = autocompleteText;
	}

	public void SetMaxNumeralAmount(int newMaxNumeralAmount)
	{
		SetMaxNumeralAmount((long)newMaxNumeralAmount);
	}

	public void SetMaxNumeralAmount(long newMaxNumeralAmount)
	{
		maxNumeralAmount = newMaxNumeralAmount;
		maxFloatNumeralAmount = 0f;
	}

	public void SetMaxNumeralAmount(float newMaxNumeralAmount)
	{
		if (!allowDecimalNumbers)
		{
			SetMaxNumeralAmount((long)Math.Floor(newMaxNumeralAmount));
			return;
		}
		maxFloatNumeralAmount = newMaxNumeralAmount;
		maxNumeralAmount = 0L;
	}

	public void ClearText()
	{
		tmpInputField.text = "";
		_lastFormattedValue = "";
	}

	private void NumeralFormatter(string input)
	{
		bool isReplacingSelection = _isReplacingSelection;
		_isReplacingSelection = false;
		if (string.IsNullOrEmpty(input) && !string.IsNullOrEmpty(_lastFormattedValue))
		{
			if (!tmpInputField.isFocused)
			{
				ResetTextField();
			}
			else
			{
				_lastFormattedValue = "";
			}
			return;
		}
		NumberFormatInfo numberFormat = NumberFormat;
		string currencySymbol = numberFormat.CurrencySymbol;
		string numberDecimalSeparator = numberFormat.NumberDecimalSeparator;
		string numberGroupSeparator = numberFormat.NumberGroupSeparator;
		if (_lastFormattedValue == input)
		{
			return;
		}
		if (!allowNegativeValues && !string.IsNullOrEmpty(input) && input.Contains("-"))
		{
			RevertToLastValidValue(tmpInputField.caretPosition);
			return;
		}
		input = input.Replace(currencySymbol, "");
		string text = ((numberDecimalSeparator == ".") ? "," : ".");
		string text2 = _lastFormattedValue.Replace(currencySymbol, "");
		if (text2.IndexOf(text, StringComparison.Ordinal) < 0)
		{
			input = NormalizeAlternateDecimalSeparator(input, numberDecimalSeparator, text);
		}
		int length = input.Length;
		int length2 = text2.Length;
		int num = tmpInputField.caretPosition;
		int numberOfCommasBeforeFormat = CountCharactersBefore(input, num, numberGroupSeparator);
		bool flag = false;
		if (!isReplacingSelection && length < length2 && tmpInputField.isFocused)
		{
			int num2 = length;
			for (int i = 0; i < length; i++)
			{
				if (input[i] != text2[i])
				{
					num2 = i;
					break;
				}
			}
			_lastCaretPosition--;
			bool flag2 = num2 == _lastCaretPosition - 1 || num2 == _lastCaretPosition || num2 == length;
			if (flag2 && showCurrencySign && num2 == 0 && _lastCaretPosition == 0)
			{
				RevertToLastValidValue(num);
				return;
			}
			if (num2 >= 0 && num2 < length2)
			{
				string text3 = text2[num2].ToString();
				if (flag2 && text3 != numberGroupSeparator && text3 != numberDecimalSeparator && input.IndexOf(numberDecimalSeparator, StringComparison.Ordinal) >= 0)
				{
					int num3 = text2.IndexOf(numberDecimalSeparator, StringComparison.Ordinal);
					if (num3 >= 0 && num2 > num3)
					{
						num = num2;
						if (showCurrencySign)
						{
							num += currencySymbol.Length;
						}
						flag = true;
					}
				}
				if (text3 == numberDecimalSeparator)
				{
					if (flag2)
					{
						int num4 = Math.Max(0, num2 - 1);
						input = input.Remove(num4, 1).Insert(num4, numberDecimalSeparator);
						length = input.Length;
						num = num4;
					}
					else
					{
						input = input.Substring(0, num2);
						length = input.Length;
						num = num2;
					}
					if (showCurrencySign)
					{
						num += currencySymbol.Length;
					}
				}
				else if (text3 == numberGroupSeparator)
				{
					int num5;
					if (flag2)
					{
						num5 = Math.Max(0, num2 - 1);
						num--;
					}
					else
					{
						num5 = Math.Max(0, num2);
						num = num5;
						if (showCurrencySign)
						{
							num += currencySymbol.Length;
						}
					}
					input = input.Remove(num5, 1);
				}
			}
		}
		string pattern = (allowNegativeValues ? ("[^\\d" + Regex.Escape(numberDecimalSeparator) + "-]") : ("[^\\d" + Regex.Escape(numberDecimalSeparator) + "]"));
		string text4 = Regex.Replace(input, pattern, "");
		if (string.IsNullOrWhiteSpace(text4))
		{
			ResetTextField();
			return;
		}
		bool num6 = Regex.IsMatch(text4, "^[0" + Regex.Escape(numberDecimalSeparator) + "]+$");
		bool flag3 = allowDecimalNumbers && input.StartsWith(numberDecimalSeparator, StringComparison.Ordinal);
		bool flag4 = allowDecimalNumbers && text2.StartsWith("0" + numberDecimalSeparator, StringComparison.Ordinal) && input.StartsWith("0", StringComparison.Ordinal) && input.IndexOf(numberDecimalSeparator, StringComparison.Ordinal) == num;
		if (num6 && !flag3 && num < length && length < length2)
		{
			while (input.StartsWith(numberDecimalSeparator, StringComparison.Ordinal))
			{
				string text5 = input;
				int length3 = numberDecimalSeparator.Length;
				input = text5.Substring(length3, text5.Length - length3);
			}
			input = AddCurrencySymbolIfNeeded(input);
			UpdateTextField(input, num);
			return;
		}
		if (input.StartsWith(numberDecimalSeparator, StringComparison.Ordinal))
		{
			input = "0" + input;
			text4 = "0" + text4;
			num++;
		}
		else if (allowNegativeValues && input.StartsWith("-" + numberDecimalSeparator, StringComparison.Ordinal))
		{
			string text5 = input;
			input = "-0" + text5.Substring(1, text5.Length - 1);
			text5 = text4;
			text4 = "-0" + text5.Substring(1, text5.Length - 1);
			num++;
		}
		bool flag5 = input.Contains(numberDecimalSeparator);
		if ((flag5 && input.IndexOf(numberDecimalSeparator, StringComparison.Ordinal) != input.LastIndexOf(numberDecimalSeparator, StringComparison.Ordinal)) || (!allowDecimalNumbers & flag5))
		{
			RevertToLastValidValue(num);
			return;
		}
		if (flag4)
		{
			num--;
		}
		if (TryParseNumeralShortcut(input, out var output))
		{
			UpdateTextField(output, output.Length);
			tmpInputField.caretPosition = (showCurrencySign ? (output.Length + currencySymbol.Length) : output.Length);
			return;
		}
		if (!TryParseValue(text4, flag5, out var value))
		{
			RevertToLastValidValue(num);
			return;
		}
		if (GetMaxNumeralAmount(out var maxNumeralValue))
		{
			value = Math.Min(value, maxNumeralValue);
		}
		bool flag6 = flag5 && input.EndsWith(numberDecimalSeparator);
		int num7 = 0;
		if (flag5 && !flag6)
		{
			int num8 = text4.IndexOf(numberDecimalSeparator, StringComparison.Ordinal);
			num7 = text4.Length - num8 - numberDecimalSeparator.Length;
			num7 = Mathf.Clamp(num7, 0, 2);
		}
		string text6 = ((allowDecimalNumbers && !flag5) ? "N2" : ((flag5 && !flag6) ? $"N{num7}" : "N0"));
		string text7 = value.ToString(text6, CultureHelper.CultureInfo);
		if (flag6)
		{
			text7 += numberDecimalSeparator;
		}
		string text8 = AddCurrencySymbolIfNeeded(text7);
		if (tmpInputField.text != text8)
		{
			AdjustCaretAndSetText(text8, num, numberOfCommasBeforeFormat);
			return;
		}
		_lastFormattedValue = text8;
		if (flag)
		{
			tmpInputField.caretPosition = Math.Min(text8.Length, num);
		}
	}

	private void NumeralEndEditFormatter(string input)
	{
		if (string.IsNullOrEmpty(input))
		{
			ResetTextField();
		}
		else
		{
			if (!allowDecimalNumbers)
			{
				return;
			}
			string rawValue = GetRawValue();
			string numberDecimalSeparator = NumberFormat.NumberDecimalSeparator;
			string text = ((numberDecimalSeparator == ".") ? "," : ".");
			if (!rawValue.Contains(numberDecimalSeparator) && !ShouldTreatAsDecimalSeparator(rawValue, text))
			{
				return;
			}
			rawValue = NormalizeAlternateDecimalSeparator(rawValue, numberDecimalSeparator, text);
			if (decimal.TryParse(rawValue, NumberStyles.Number, CultureHelper.CultureInfo, out var result))
			{
				if (GetMaxNumeralAmount(out var maxNumeralValue))
				{
					result = Math.Min(result, maxNumeralValue);
				}
				if (!allowNegativeValues && result < 0m)
				{
					result *= -1m;
				}
				UpdateTextField(AddCurrencySymbolIfNeeded(result.ToString("N2", CultureHelper.CultureInfo)), 0, notify: false);
			}
		}
	}

	private void ResetTextField()
	{
		UpdateTextField(AddCurrencySymbolIfNeeded("0"), 1, notify: false);
	}

	private void UpdateTextField(string text, int caretPosition, bool notify = true)
	{
		SetText(text, notify);
		tmpInputField.caretPosition = caretPosition;
	}

	private void RevertToLastValidValue(int caretPositionBeforeFormat)
	{
		int caretPosition = Math.Min(_lastFormattedValue.Length, caretPositionBeforeFormat);
		UpdateTextField(_lastFormattedValue, caretPosition);
	}

	private string AddCurrencySymbolIfNeeded(string text)
	{
		if (!showCurrencySign)
		{
			return text;
		}
		return text.AddCurrencySymbol();
	}

	private static string GetInvariantNumberInput(string input)
	{
		NumberFormatInfo numberFormat = NumberFormat;
		return input.Replace(numberFormat.NumberGroupSeparator, "").Replace(numberFormat.NumberDecimalSeparator, ".");
	}

	private bool TryParseValue(string digitsOnly, bool hasDecimal, out decimal value)
	{
		string text = GetInvariantNumberInput(digitsOnly);
		if (hasDecimal)
		{
			string[] array = text.Split('.');
			if (array.Length > 1 && array[1].Length > 2)
			{
				text = array[0] + "." + array[1].Substring(0, 2);
			}
			if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value))
			{
				return false;
			}
			if (!allowNegativeValues && value < 0m)
			{
				value *= -1m;
			}
			return true;
		}
		bool result;
		if (allowDecimalNumbers)
		{
			result = decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out value);
		}
		else
		{
			result = long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result2);
			value = result2;
		}
		if (!allowNegativeValues && value < 0m)
		{
			value *= -1m;
		}
		return result;
	}

	private void AdjustCaretAndSetText(string formattedText, int caretPositionBeforeFormat, int numberOfCommasBeforeFormat)
	{
		int num = CountCharactersBefore(formattedText, caretPositionBeforeFormat, NumberFormat.NumberGroupSeparator);
		int num2 = caretPositionBeforeFormat + (num - numberOfCommasBeforeFormat);
		string currencySymbol = NumberFormat.CurrencySymbol;
		if (showCurrencySign && formattedText.StartsWith(currencySymbol, StringComparison.Ordinal))
		{
			int length = currencySymbol.Length;
			if (caretPositionBeforeFormat <= length)
			{
				num2 = Math.Min(num2, length);
			}
		}
		UpdateTextField(formattedText, Math.Min(formattedText.Length, num2), notify: false);
	}

	private static int CountCharactersBefore(string text, int endIndex, string character)
	{
		int num = 0;
		int num2 = Math.Min(text.Length, endIndex);
		for (int i = 0; i < num2; i++)
		{
			if (text[i].ToString() == character)
			{
				num++;
			}
		}
		return num;
	}

	private string NormalizeAlternateDecimalSeparator(string input, string numberDecimalSeparator, string alternateDecimalSeparator)
	{
		if (!allowDecimalNumbers || input.Contains(numberDecimalSeparator) || !ShouldTreatAsDecimalSeparator(input, alternateDecimalSeparator))
		{
			return input;
		}
		int num = input.IndexOf(alternateDecimalSeparator, StringComparison.Ordinal);
		string text = input.Substring(0, num);
		int num2 = num + alternateDecimalSeparator.Length;
		return text + numberDecimalSeparator + input.Substring(num2, input.Length - num2);
	}

	private static bool ShouldTreatAsDecimalSeparator(string input, string separator)
	{
		int num = input.IndexOf(separator, StringComparison.Ordinal);
		if (num < 0 || num != input.LastIndexOf(separator, StringComparison.Ordinal))
		{
			return false;
		}
		int num2 = 0;
		for (int i = num + separator.Length; i < input.Length; i++)
		{
			if (char.IsDigit(input[i]))
			{
				num2++;
			}
		}
		return num2 <= 2;
	}

	private bool TryParseNumeralShortcut(string input, out string output)
	{
		output = "";
		string text = GetInvariantNumberInput(input).Trim().ToUpperInvariant();
		if (float.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var _) || !text.FromShortCurrencyFormatInvariant(out var value))
		{
			return false;
		}
		if (!allowNegativeValues)
		{
			value = Math.Abs(value);
		}
		if (GetMaxNumeralAmount(out var maxNumeralValue) && value > maxNumeralValue)
		{
			value = maxNumeralValue;
		}
		output = value.ToString(allowDecimalNumbers ? "N2" : "N0", CultureHelper.CultureInfo);
		return true;
	}

	private bool GetMaxNumeralAmount(out decimal maxNumeralValue)
	{
		if (allowDecimalNumbers && maxFloatNumeralAmount > 0f)
		{
			maxNumeralValue = (decimal)maxFloatNumeralAmount;
			return true;
		}
		maxNumeralValue = maxNumeralAmount;
		return maxNumeralAmount > 0;
	}

	public string GetRawValue()
	{
		if (formattingType != FormattingType.Number)
		{
			return tmpInputField.text;
		}
		NumberFormatInfo numberFormat = NumberFormat;
		return tmpInputField.text.Replace(numberFormat.CurrencySymbol, "").Replace(numberFormat.NumberGroupSeparator, "");
	}
}
