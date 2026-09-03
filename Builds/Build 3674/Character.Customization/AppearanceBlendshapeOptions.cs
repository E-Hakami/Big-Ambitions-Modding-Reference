using System;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters;
using BigAmbitions.Characters.Appearance;
using Extensions;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Character.Customization;

public class AppearanceBlendshapeOptions : MonoBehaviour
{
	[Header("UI")]
	[SerializeField]
	private Transform sliderTemplate;

	[SerializeField]
	private Button resetButton;

	[SerializeField]
	private bool resetToCurrentCharacter;

	[SerializeField]
	private GameObject panel;

	[SerializeField]
	private GameObject eyesColorPicker;

	[Header("Data")]
	[SerializeField]
	private AppearanceSetter appearanceSetter;

	[SerializeField]
	private BlendshapeOption[] blendshapeOptions;

	private AppearanceElementType _currentElementType;

	[HideInInspector]
	public bool isVisible;

	public Action onShown;

	public Action onBlendshapeChange;

	public Action onReset;

	public bool HasOptionsForElement(AppearanceElementType elementType)
	{
		return blendshapeOptions.Any((BlendshapeOption option) => option.elementType == elementType);
	}

	private void Start()
	{
		resetButton.onClick.AddListener(resetToCurrentCharacter ? new UnityAction(ResetToCurrentCharacter) : new UnityAction(ResetToDefault));
	}

	private void ResetToDefault()
	{
		if (appearanceSetter?.data?.blendshapes == null)
		{
			return;
		}
		BlendshapeOption[] array = blendshapeOptions;
		foreach (BlendshapeOption option in array)
		{
			if (option.elementType != _currentElementType)
			{
				continue;
			}
			if (option.isAffectingTwoBlendshapes)
			{
				FacialBlendshape facialBlendshape = appearanceSetter.data.blendshapes.FirstOrDefault((FacialBlendshape bs) => bs.name == option.blendshapeNameLow);
				if (facialBlendshape != null)
				{
					facialBlendshape.value = 0f;
				}
				FacialBlendshape facialBlendshape2 = appearanceSetter.data.blendshapes.FirstOrDefault((FacialBlendshape bs) => bs.name == option.blendshapeNameHigh);
				if (facialBlendshape2 != null)
				{
					facialBlendshape2.value = 0f;
				}
			}
			else
			{
				FacialBlendshape facialBlendshape3 = appearanceSetter.data.blendshapes.FirstOrDefault((FacialBlendshape bs) => bs.name == option.blendshapeName);
				if (facialBlendshape3 != null)
				{
					facialBlendshape3.value = 0f;
				}
			}
		}
		appearanceSetter.UpdateVisuals();
		onReset?.Invoke();
		Show(show: true, _currentElementType);
	}

	public void ResetToCurrentCharacter()
	{
		CharacterData characterData = SaveGameManager.Current.charactersData.First();
		CharacterData characterData2 = characterData;
		if (characterData2.blendshapes == null)
		{
			characterData2.blendshapes = new List<FacialBlendshape>();
		}
		characterData2 = appearanceSetter.data;
		if (characterData2.blendshapes == null)
		{
			characterData2.blendshapes = new List<FacialBlendshape>(characterData.blendshapes.Count);
		}
		BlendshapeOption[] array = blendshapeOptions;
		foreach (BlendshapeOption blendshapeOption in array)
		{
			if (_currentElementType == AppearanceElementType.Gender || blendshapeOption.elementType == _currentElementType)
			{
				if (blendshapeOption.isAffectingTwoBlendshapes)
				{
					MatchBlendshape(blendshapeOption.blendshapeNameLow);
					MatchBlendshape(blendshapeOption.blendshapeNameHigh);
				}
				else
				{
					MatchBlendshape(blendshapeOption.blendshapeName);
				}
			}
		}
		if (_currentElementType == AppearanceElementType.Eyes)
		{
			appearanceSetter.data.eyesColor = characterData.eyesColor;
		}
		appearanceSetter.UpdateVisuals();
		onReset?.Invoke();
		Show(isVisible, _currentElementType);
	}

	private void MatchBlendshape(string matchName)
	{
		FacialBlendshape facialBlendshape = SaveGameManager.Current.charactersData.First().blendshapes.FirstOrDefault((FacialBlendshape bs) => bs.name == matchName);
		if (facialBlendshape == null)
		{
			appearanceSetter.data.blendshapes.RemoveAll((FacialBlendshape bs) => bs.name == matchName);
		}
		else
		{
			GetOrAddBlendshape(matchName).value = facialBlendshape.value;
		}
	}

	private FacialBlendshape GetOrAddBlendshape(string blendshapeName)
	{
		FacialBlendshape facialBlendshape = appearanceSetter.data.blendshapes.FirstOrDefault((FacialBlendshape bs) => bs.name == blendshapeName);
		if (facialBlendshape == null)
		{
			facialBlendshape = new FacialBlendshape
			{
				name = blendshapeName,
				value = 0f
			};
			appearanceSetter.data.blendshapes.Add(facialBlendshape);
		}
		return facialBlendshape;
	}

	public void Show(bool show, AppearanceElementType elementType)
	{
		if ((object)appearanceSetter == null)
		{
			appearanceSetter = InstanceBehavior<GameManager>.Instance.employeeUniformPreview.appearanceSetter;
		}
		_currentElementType = elementType;
		int num = blendshapeOptions.Count((BlendshapeOption option) => option.elementType == elementType);
		if (show && num > 0)
		{
			resetButton.gameObject.SetActive(value: true);
			panel.SetActive(value: true);
			sliderTemplate.ResetTemplate();
			BlendshapeOption[] array = blendshapeOptions;
			foreach (BlendshapeOption blendshapeOption in array)
			{
				if (blendshapeOption.elementType == elementType)
				{
					CreateSlider(blendshapeOption);
				}
			}
		}
		else
		{
			resetButton.gameObject.SetActive(value: false);
			panel.SetActive(value: false);
		}
		eyesColorPicker.SetActive(elementType == AppearanceElementType.Eyes || elementType == AppearanceElementType.Head);
		isVisible = show;
		if (isVisible)
		{
			onShown?.Invoke();
		}
	}

	private void CreateSlider(BlendshapeOption option)
	{
		if (appearanceSetter?.data?.blendshapes == null)
		{
			return;
		}
		float value;
		if (option.isAffectingTwoBlendshapes)
		{
			FacialBlendshape facialBlendshape = appearanceSetter.data.blendshapes.FirstOrDefault((FacialBlendshape bs) => bs.name == option.blendshapeNameLow);
			FacialBlendshape facialBlendshape2 = appearanceSetter.data.blendshapes.FirstOrDefault((FacialBlendshape bs) => bs.name == option.blendshapeNameHigh);
			value = (((facialBlendshape != null) ? new float?(0f - facialBlendshape.value) : ((float?)null)) + facialBlendshape2?.value).GetValueOrDefault();
		}
		else
		{
			value = appearanceSetter.data.blendshapes.FirstOrDefault((FacialBlendshape bs) => bs.name == option.blendshapeName)?.value ?? 0f;
		}
		SliderWithHeader component = UnityEngine.Object.Instantiate(sliderTemplate, sliderTemplate.parent).GetComponent<SliderWithHeader>();
		component.SetUp(option.headerKey, value, option.isAffectingTwoBlendshapes ? (-100) : 0, 100f, delegate(float value2)
		{
			SetBlendshape(option, value2);
		});
		component.gameObject.SetActive(value: true);
	}

	private void SetBlendshape(BlendshapeOption option, float value)
	{
		if (appearanceSetter?.data?.blendshapes == null)
		{
			return;
		}
		if (option.isAffectingTwoBlendshapes)
		{
			float value2 = Mathf.Max(0f - value, 0f);
			float value3 = Mathf.Max(value, 0f);
			FacialBlendshape facialBlendshape = appearanceSetter.data.blendshapes.FirstOrDefault((FacialBlendshape bs) => bs.name == option.blendshapeNameLow);
			FacialBlendshape facialBlendshape2 = appearanceSetter.data.blendshapes.FirstOrDefault((FacialBlendshape bs) => bs.name == option.blendshapeNameHigh);
			if (facialBlendshape == null)
			{
				facialBlendshape = new FacialBlendshape
				{
					name = option.blendshapeNameLow,
					value = value2
				};
				appearanceSetter.data.blendshapes.Add(facialBlendshape);
			}
			facialBlendshape.value = value2;
			if (facialBlendshape2 == null)
			{
				facialBlendshape2 = new FacialBlendshape
				{
					name = option.blendshapeNameHigh,
					value = value3
				};
				appearanceSetter.data.blendshapes.Add(facialBlendshape2);
			}
			facialBlendshape2.value = value3;
		}
		else
		{
			FacialBlendshape facialBlendshape3 = appearanceSetter.data.blendshapes.FirstOrDefault((FacialBlendshape bs) => bs.name == option.blendshapeName);
			if (facialBlendshape3 == null)
			{
				facialBlendshape3 = new FacialBlendshape
				{
					name = option.blendshapeName,
					value = value
				};
				appearanceSetter.data.blendshapes.Add(facialBlendshape3);
			}
			facialBlendshape3.value = value;
		}
		appearanceSetter.UpdateVisuals();
		onBlendshapeChange?.Invoke();
	}

	public bool HaveBlendshapesChanged(AppearanceElementType elementType)
	{
		List<FacialBlendshape> blendshapes = SaveGameManager.Current.charactersData.First().blendshapes;
		List<FacialBlendshape> blendshapes2 = appearanceSetter.data.blendshapes;
		BlendshapeOption[] array = blendshapeOptions;
		foreach (BlendshapeOption option in array)
		{
			if (option.elementType != elementType)
			{
				continue;
			}
			if (option.isAffectingTwoBlendshapes)
			{
				float num = blendshapes.FirstOrDefault((FacialBlendshape bs) => bs.name == option.blendshapeNameLow)?.value ?? 0f;
				float num2 = blendshapes.FirstOrDefault((FacialBlendshape bs) => bs.name == option.blendshapeNameHigh)?.value ?? 0f;
				float num3 = blendshapes2.FirstOrDefault((FacialBlendshape bs) => bs.name == option.blendshapeNameLow)?.value ?? 0f;
				float num4 = blendshapes2.FirstOrDefault((FacialBlendshape bs) => bs.name == option.blendshapeNameHigh)?.value ?? 0f;
				if (Mathf.Abs(num - num3) > 5f)
				{
					return true;
				}
				if (Mathf.Abs(num2 - num4) > 5f)
				{
					return true;
				}
			}
			else
			{
				float num5 = blendshapes.FirstOrDefault((FacialBlendshape bs) => bs.name == option.blendshapeName)?.value ?? 0f;
				float num6 = blendshapes2.FirstOrDefault((FacialBlendshape bs) => bs.name == option.blendshapeName)?.value ?? 0f;
				if (Mathf.Abs(num5 - num6) > 5f)
				{
					return true;
				}
			}
		}
		return false;
	}
}
