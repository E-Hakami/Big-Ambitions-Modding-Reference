using System;
using NaughtyAttributes;
using UI.Components;
using UnityEngine;
using UnityEngine.Events;

namespace UI.MainMenu;

public class CustomGameOptionsHandler : MonoBehaviour
{
	[HideInInspector]
	public UnityEvent onValueChanged = new UnityEvent();

	[BoxGroup("General")]
	[SerializeField]
	private CustomGameSliderOption daysPerYearSlider;

	[BoxGroup("General")]
	[SerializeField]
	private InputField startingMoneyInputField;

	[BoxGroup("General")]
	[SerializeField]
	private CustomGameSliderOption rivalAttacksMultiplierSlider;

	[BoxGroup("Character")]
	[SerializeField]
	private CustomGameSliderOption startingAgeSlider;

	[BoxGroup("Character")]
	[SerializeField]
	private CustomGameCheckboxOption disableAgingToggle;

	[BoxGroup("Character")]
	[SerializeField]
	private CustomGameCheckboxOption disableHappinessToggle;

	[BoxGroup("Character")]
	[SerializeField]
	private CustomGameCheckboxOption disableEnergyToggle;

	[BoxGroup("Character")]
	[SerializeField]
	private CustomGameCheckboxOption allCoursesUnlockedToggle;

	[BoxGroup("Character")]
	[SerializeField]
	private CustomGameCheckboxOption allContactsUnlockedToggle;

	[BoxGroup("Character")]
	[SerializeField]
	private CustomGameCheckboxOption disableVehicleDamageToggle;

	[BoxGroup("Character")]
	[SerializeField]
	private CustomGameCheckboxOption disableVehicleFuelToggle;

	[BoxGroup("Economy")]
	[SerializeField]
	private CustomGameSliderOption taxPercentageSlider;

	[BoxGroup("Economy")]
	[SerializeField]
	private CustomGameSliderOption employeeHourlySalaryMultiplierSlider;

	[BoxGroup("Economy")]
	[SerializeField]
	private CustomGameSliderOption publicPricesMultiplierSlider;

	[BoxGroup("Economy")]
	[SerializeField]
	private CustomGameSliderOption bankInterestMultiplierSlider;

	[BoxGroup("Economy")]
	[SerializeField]
	private CustomGameSliderOption baseCustomerPromotionMultiplierSlider;

	[BoxGroup("Economy")]
	[SerializeField]
	private CustomGameSliderOption wholesaleUrgentFeeMultiplierSlider;

	[BoxGroup("Economy")]
	[SerializeField]
	private CustomGameSliderOption importerUrgentFeeMultiplierSlider;

	[BoxGroup("Economy")]
	[SerializeField]
	private CustomGameCheckboxOption disableWholesaleAndImportLimitsToggle;

	[BoxGroup("Economy")]
	[SerializeField]
	private CustomGameCheckboxOption allProductsAvailableFromImportersToggle;

	[BoxGroup("Economy")]
	[SerializeField]
	private CustomGameSliderOption exportMultiplierSlider;

	public static GameVariables currentPreset;

	[NonSerialized]
	public bool preventAutoSetOnStart;

	private void Start()
	{
		InitializeGeneralSettingsOptions();
		InitializeCharacterSettingsOptions();
		InitializeEconomySettingsOptions();
		if (!preventAutoSetOnStart)
		{
			SetPreset(InstanceBehavior<GlobalReferences>.Instance.difficultySettings[0].ToGameVariables());
		}
	}

	private void InitializeGeneralSettingsOptions()
	{
		daysPerYearSlider.onValueChanged = delegate(float newValue)
		{
			int daysPerYear = (int)newValue;
			currentPreset.daysPerYear = daysPerYear;
			onValueChanged.Invoke();
		};
		startingMoneyInputField.tmpInputField.onValueChanged.AddListener(delegate
		{
			if (float.TryParse(startingMoneyInputField.GetRawValue(), out var result))
			{
				if (result > 2E+09f)
				{
					currentPreset.startingMoney = 2000000000;
				}
				else
				{
					currentPreset.startingMoney = (int)result;
				}
				onValueChanged.Invoke();
			}
		});
		rivalAttacksMultiplierSlider.onValueChanged = delegate(float newValue)
		{
			currentPreset.rivalsDifficultyMultiplier = newValue;
			onValueChanged.Invoke();
		};
	}

	private void InitializeCharacterSettingsOptions()
	{
		startingAgeSlider.onValueChanged = delegate(float newValue)
		{
			int startingAge = (int)newValue;
			currentPreset.startingAge = startingAge;
			onValueChanged.Invoke();
		};
		disableAgingToggle.onValueChanged = delegate(bool newValue)
		{
			currentPreset.disableAging = newValue;
			onValueChanged.Invoke();
		};
		disableEnergyToggle.onValueChanged = delegate(bool newValue)
		{
			currentPreset.disableEnergy = newValue;
			onValueChanged.Invoke();
		};
		disableHappinessToggle.onValueChanged = delegate(bool newValue)
		{
			currentPreset.disableHappiness = newValue;
			onValueChanged.Invoke();
		};
		allCoursesUnlockedToggle.onValueChanged = delegate(bool newValue)
		{
			currentPreset.allCoursesUnlocked = newValue;
			onValueChanged.Invoke();
		};
		allContactsUnlockedToggle.onValueChanged = delegate(bool newValue)
		{
			currentPreset.allContactsUnlocked = newValue;
			onValueChanged.Invoke();
		};
		disableVehicleDamageToggle.onValueChanged = delegate(bool newValue)
		{
			currentPreset.disableVehicleDamage = newValue;
			onValueChanged.Invoke();
		};
		disableVehicleFuelToggle.onValueChanged = delegate(bool newValue)
		{
			currentPreset.disableVehicleFuel = newValue;
			onValueChanged.Invoke();
		};
	}

	private void InitializeEconomySettingsOptions()
	{
		taxPercentageSlider.onValueChanged = delegate(float newValue)
		{
			currentPreset.taxPercentage = (int)newValue;
			onValueChanged.Invoke();
		};
		publicPricesMultiplierSlider.onValueChanged = delegate(float newValue)
		{
			currentPreset.marketPriceMultiplier = newValue;
			onValueChanged.Invoke();
		};
		employeeHourlySalaryMultiplierSlider.onValueChanged = delegate(float newValue)
		{
			currentPreset.employeeHourlySalaryMultiplier = newValue;
			onValueChanged.Invoke();
		};
		bankInterestMultiplierSlider.onValueChanged = delegate(float newValue)
		{
			currentPreset.bankInterestMultiplier = newValue;
			onValueChanged.Invoke();
		};
		baseCustomerPromotionMultiplierSlider.onValueChanged = delegate(float newValue)
		{
			currentPreset.baseCustomerPromotionMultiplier = newValue;
			onValueChanged.Invoke();
		};
		wholesaleUrgentFeeMultiplierSlider.onValueChanged = delegate(float newValue)
		{
			currentPreset.wholesaleUrgentFeeMultiplier = newValue;
			onValueChanged.Invoke();
		};
		importerUrgentFeeMultiplierSlider.onValueChanged = delegate(float newValue)
		{
			currentPreset.importerUrgentFeeMultiplier = newValue;
			onValueChanged.Invoke();
		};
		disableWholesaleAndImportLimitsToggle.onValueChanged = delegate(bool newValue)
		{
			currentPreset.disableWholesaleAndImportLimits = newValue;
			onValueChanged.Invoke();
		};
		allProductsAvailableFromImportersToggle.onValueChanged = delegate(bool newValue)
		{
			currentPreset.allProductsAvailableFromImporters = newValue;
			onValueChanged.Invoke();
		};
		exportMultiplierSlider.onValueChanged = delegate(float newValue)
		{
			currentPreset.exportMultiplier = newValue;
			onValueChanged.Invoke();
		};
	}

	public void SetPreset(GameVariables preset)
	{
		currentPreset = preset.Copy();
		currentPreset.difficulty = Difficulty.Custom;
		currentPreset.tutorialEnabled = false;
		daysPerYearSlider.SetValue(currentPreset.daysPerYear);
		startingMoneyInputField.tmpInputField.text = currentPreset.startingMoney.ToString();
		rivalAttacksMultiplierSlider.SetValue(currentPreset.rivalsDifficultyMultiplier);
		startingAgeSlider.SetValue(currentPreset.startingAge);
		disableAgingToggle.SetValue(currentPreset.disableAging);
		disableEnergyToggle.SetValue(currentPreset.disableEnergy);
		disableHappinessToggle.SetValue(currentPreset.disableHappiness);
		allCoursesUnlockedToggle.SetValue(currentPreset.allCoursesUnlocked);
		allContactsUnlockedToggle.SetValue(currentPreset.allContactsUnlocked);
		disableVehicleDamageToggle.SetValue(currentPreset.disableVehicleDamage);
		disableVehicleFuelToggle.SetValue(currentPreset.disableVehicleFuel);
		taxPercentageSlider.SetValue(currentPreset.taxPercentage);
		employeeHourlySalaryMultiplierSlider.SetValue(currentPreset.employeeHourlySalaryMultiplier);
		publicPricesMultiplierSlider.SetValue(currentPreset.marketPriceMultiplier);
		bankInterestMultiplierSlider.SetValue(currentPreset.bankInterestMultiplier);
		baseCustomerPromotionMultiplierSlider.SetValue(currentPreset.baseCustomerPromotionMultiplier);
		wholesaleUrgentFeeMultiplierSlider.SetValue(currentPreset.wholesaleUrgentFeeMultiplier);
		importerUrgentFeeMultiplierSlider.SetValue(currentPreset.importerUrgentFeeMultiplier);
		disableWholesaleAndImportLimitsToggle.SetValue(currentPreset.disableWholesaleAndImportLimits);
		allProductsAvailableFromImportersToggle.SetValue(currentPreset.allProductsAvailableFromImporters);
		exportMultiplierSlider.SetValue(currentPreset.exportMultiplier);
	}

	public static GameVariables GetPreset()
	{
		return currentPreset;
	}

	public GameVariables EnsureLimits(GameVariables preset)
	{
		preset.startingAge = (int)Mathf.Clamp(preset.startingAge, startingAgeSlider.MinValue, startingAgeSlider.MaxValue);
		preset.startingMoney = Mathf.Max(preset.startingMoney, 0);
		preset.daysPerYear = (int)Mathf.Clamp(preset.daysPerYear, daysPerYearSlider.MinValue, daysPerYearSlider.MaxValue);
		preset.taxPercentage = (int)Mathf.Clamp(preset.taxPercentage, taxPercentageSlider.MinValue, taxPercentageSlider.MaxValue);
		preset.rivalsDifficultyMultiplier = Mathf.Clamp(preset.rivalsDifficultyMultiplier, rivalAttacksMultiplierSlider.MinValue, rivalAttacksMultiplierSlider.MaxValue);
		preset.marketPriceMultiplier = Mathf.Clamp(preset.marketPriceMultiplier, publicPricesMultiplierSlider.MinValue, publicPricesMultiplierSlider.MaxValue);
		preset.employeeHourlySalaryMultiplier = Mathf.Clamp(preset.employeeHourlySalaryMultiplier, employeeHourlySalaryMultiplierSlider.MinValue, employeeHourlySalaryMultiplierSlider.MaxValue);
		preset.bankInterestMultiplier = Mathf.Clamp(preset.bankInterestMultiplier, bankInterestMultiplierSlider.MinValue, bankInterestMultiplierSlider.MaxValue);
		preset.baseCustomerPromotionMultiplier = Mathf.Clamp(preset.baseCustomerPromotionMultiplier, baseCustomerPromotionMultiplierSlider.MinValue, baseCustomerPromotionMultiplierSlider.MaxValue);
		preset.wholesaleUrgentFeeMultiplier = Mathf.Clamp(preset.wholesaleUrgentFeeMultiplier, wholesaleUrgentFeeMultiplierSlider.MinValue, wholesaleUrgentFeeMultiplierSlider.MaxValue);
		preset.importerUrgentFeeMultiplier = Mathf.Clamp(preset.importerUrgentFeeMultiplier, importerUrgentFeeMultiplierSlider.MinValue, importerUrgentFeeMultiplierSlider.MaxValue);
		preset.exportMultiplier = Mathf.Clamp(preset.exportMultiplier, exportMultiplierSlider.MinValue, exportMultiplierSlider.MaxValue);
		return preset;
	}
}
