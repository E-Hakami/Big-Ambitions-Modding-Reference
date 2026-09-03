using System;
using System.Collections.Generic;
using Extensions;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace Tooltip;

public class TooltipSystem : InstanceBehavior<TooltipSystem>
{
	public const float Delay = 0.1f;

	private const float OffsetInPixelsBetweenCursorAndPanel = 40f;

	private static bool IsPaused;

	[SerializeField]
	private Canvas canvas;

	[SerializeField]
	private RectTransform canvasRectTransform;

	[SerializeField]
	private RectTransform tooltipRect;

	[SerializeField]
	private CanvasGroup canvasGroup;

	[SerializeField]
	private Transform dataTransform;

	[SerializeField]
	private RectTransform dataRectTransform;

	[SerializeField]
	private Transform splitterTemplate;

	[SerializeField]
	private Transform headerTemplate;

	[SerializeField]
	private Transform categoryTemplate;

	[SerializeField]
	private Transform labelTemplate;

	[SerializeField]
	private Transform progressBarTemplate;

	[SerializeField]
	private Transform keyValueListTemplate;

	[SerializeField]
	private Transform listTemplate;

	[SerializeField]
	private Transform checkboxListTemplate;

	[SerializeField]
	private Transform demandsListTemplate;

	[SerializeField]
	private Image imageTemplate;

	private Vector3 _lastMousePosition;

	public static bool IsVisible { get; private set; }

	protected override void Awake()
	{
		base.Awake();
		if (base.IsMainInstance)
		{
			base.transform.SetParent(null);
			UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
		}
	}

	public void Start()
	{
		canvasGroup.alpha = 0f;
		dataTransform.gameObject.SetActive(value: true);
		splitterTemplate.gameObject.SetActive(value: false);
		headerTemplate.gameObject.SetActive(value: false);
		categoryTemplate.gameObject.SetActive(value: false);
		labelTemplate.gameObject.SetActive(value: false);
		progressBarTemplate.gameObject.SetActive(value: false);
		keyValueListTemplate.gameObject.SetActive(value: false);
		keyValueListTemplate.Find("Entry").gameObject.SetActive(value: false);
		listTemplate.gameObject.SetActive(value: false);
		listTemplate.Find("Entry").gameObject.SetActive(value: false);
		checkboxListTemplate.gameObject.SetActive(value: false);
		checkboxListTemplate.Find("Entry").gameObject.SetActive(value: false);
		if ((bool)demandsListTemplate)
		{
			demandsListTemplate.gameObject.SetActive(value: false);
			demandsListTemplate.Find("Entry").gameObject.SetActive(value: false);
		}
	}

	public void Update()
	{
		if (canvasGroup.alpha != 0f)
		{
			SetPosition();
		}
	}

	private void SetPosition()
	{
		if (!(_lastMousePosition == Input.mousePosition))
		{
			_lastMousePosition = Input.mousePosition;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRectTransform, _lastMousePosition, canvas.worldCamera, out var localPoint);
			Vector2 anchoredPosition = new Vector2(localPoint.x, localPoint.y);
			Rect rect = dataRectTransform.rect;
			Rect rect2 = canvasRectTransform.rect;
			if (anchoredPosition.x + rect2.width / 2f < rect.width / 2f)
			{
				anchoredPosition.x = (0f - rect2.width) / 2f + rect.width / 2f;
			}
			else if (anchoredPosition.x + rect2.width / 2f + rect.width / 2f > rect2.width)
			{
				anchoredPosition.x = rect2.width / 2f - rect.width / 2f;
			}
			if (anchoredPosition.y + rect2.height / 2f + rect.height > rect2.height)
			{
				anchoredPosition.y = rect2.height / 2f - rect.height;
			}
			if (localPoint.y > rect2.height / 2f - rect.height + 40f)
			{
				anchoredPosition.y = localPoint.y - rect.height - 40f;
			}
			tooltipRect.anchoredPosition = anchoredPosition;
		}
	}

	public static void PauseTooltips(bool pause)
	{
		IsPaused = pause;
		if ((bool)InstanceBehavior<TooltipSystem>.Instance & pause)
		{
			InstanceBehavior<TooltipSystem>.Instance.canvasGroup.alpha = 0f;
		}
	}

	public static void Show()
	{
		if ((bool)InstanceBehavior<TooltipSystem>.Instance)
		{
			InstanceBehavior<TooltipSystem>.Instance.ChangeState(show: true);
		}
	}

	public static void Hide()
	{
		if ((bool)InstanceBehavior<TooltipSystem>.Instance)
		{
			InstanceBehavior<TooltipSystem>.Instance.ChangeState(show: false);
		}
	}

	public static void AddSplitter()
	{
		if ((bool)InstanceBehavior<TooltipSystem>.Instance)
		{
			InstanceBehavior<TooltipSystem>.Instance.SetSplitter();
		}
	}

	public static void AddHeader(LanguageChangeEventDataHolder firstLabel, LanguageChangeEventDataHolder secondLabel = default(LanguageChangeEventDataHolder))
	{
		if ((bool)InstanceBehavior<TooltipSystem>.Instance)
		{
			InstanceBehavior<TooltipSystem>.Instance.SetHeader(firstLabel, secondLabel);
		}
	}

	public static void AddCategory(LanguageChangeEventDataHolder categoryLabel)
	{
		if ((bool)InstanceBehavior<TooltipSystem>.Instance)
		{
			InstanceBehavior<TooltipSystem>.Instance.SetCategory(categoryLabel);
		}
	}

	public static void AddLabel(LanguageChangeEventDataHolder text, Color color, FontStyles fontStyle = FontStyles.Normal, TextAlignmentOptions alignmentOptions = TextAlignmentOptions.MidlineLeft)
	{
		if ((bool)InstanceBehavior<TooltipSystem>.Instance)
		{
			InstanceBehavior<TooltipSystem>.Instance.SetLabel(text, color, fontStyle, alignmentOptions);
		}
	}

	public static void AddLabel(string text, Color color, FontStyles fontStyle = FontStyles.Normal, TextAlignmentOptions alignmentOptions = TextAlignmentOptions.MidlineLeft)
	{
		if ((bool)InstanceBehavior<TooltipSystem>.Instance)
		{
			InstanceBehavior<TooltipSystem>.Instance.SetLabel(text, color, fontStyle, alignmentOptions);
		}
	}

	public static void AddProgressBar(int progress, Color32 color)
	{
		if ((bool)InstanceBehavior<TooltipSystem>.Instance)
		{
			InstanceBehavior<TooltipSystem>.Instance.SetProgressBar(progress, color);
		}
	}

	public static void AddList(List<string> elements)
	{
		if ((bool)InstanceBehavior<TooltipSystem>.Instance)
		{
			InstanceBehavior<TooltipSystem>.Instance.SetList(elements);
		}
	}

	public static void AddList(List<(string, object)> elements)
	{
		if ((bool)InstanceBehavior<TooltipSystem>.Instance)
		{
			InstanceBehavior<TooltipSystem>.Instance.SetList(elements);
		}
	}

	public static void AddList(List<Tuple<(string, Color), string>> elements)
	{
		if ((bool)InstanceBehavior<TooltipSystem>.Instance)
		{
			InstanceBehavior<TooltipSystem>.Instance.SetList(elements);
		}
	}

	public static void AddList(List<Tuple<string, bool>> elements)
	{
		if ((bool)InstanceBehavior<TooltipSystem>.Instance)
		{
			InstanceBehavior<TooltipSystem>.Instance.SetCheckboxList(elements);
		}
	}

	public static void AddDemandList(List<(string, (string, Color), bool)> elements)
	{
		if ((bool)InstanceBehavior<TooltipSystem>.Instance)
		{
			InstanceBehavior<TooltipSystem>.Instance.SetDemandsList(elements);
		}
	}

	public static void AddImage(Sprite image)
	{
		InstanceBehavior<TooltipSystem>.Instance?.SetImage(image);
	}

	private void ChangeState(bool show)
	{
		IsVisible = show;
		ResetData();
		if (IsPaused)
		{
			return;
		}
		if (show)
		{
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				_lastMousePosition = Vector3.positiveInfinity;
				SetPosition();
				canvasGroup.alpha = 1f;
			});
		}
		else
		{
			canvasGroup.alpha = 0f;
		}
	}

	private void SetSplitter()
	{
		Transform obj = UnityEngine.Object.Instantiate(splitterTemplate, splitterTemplate.parent);
		obj.name = splitterTemplate.name + " Clone";
		obj.gameObject.SetActive(value: true);
	}

	private void SetHeader(LanguageChangeEventDataHolder firstLabel, LanguageChangeEventDataHolder secondLabel)
	{
		Transform obj = UnityEngine.Object.Instantiate(headerTemplate, headerTemplate.parent);
		GameObject gameObject = obj.Find("FirstLabel").gameObject;
		GameObject gameObject2 = obj.Find("SecondLabel").gameObject;
		GameObject obj2 = obj.Find("Filler").gameObject;
		bool flag = !string.IsNullOrEmpty(firstLabel.Key);
		gameObject.SetActive(flag);
		if (flag)
		{
			TextLocalizationComponent component = gameObject.GetComponent<TextLocalizationComponent>();
			if (LocalizorManager.IsLocalizedKey(firstLabel.Key))
			{
				component.SetData(firstLabel);
			}
			else
			{
				component.Key = null;
				gameObject.GetComponent<TMP_Text>().text = firstLabel.Key;
			}
		}
		bool flag2 = !string.IsNullOrEmpty(secondLabel.Key);
		gameObject2.SetActive(flag2);
		if (flag2)
		{
			TextLocalizationComponent component2 = gameObject2.GetComponent<TextLocalizationComponent>();
			if (LocalizorManager.IsLocalizedKey(secondLabel.Key))
			{
				component2.SetData(secondLabel);
			}
			else
			{
				component2.Key = null;
				gameObject2.GetComponent<TMP_Text>().text = secondLabel.Key;
			}
		}
		obj2.SetActive(flag & flag2);
		obj.gameObject.SetActive(value: true);
		obj.name = headerTemplate.name + " Clone";
	}

	private void SetCategory(LanguageChangeEventDataHolder categoryLabel)
	{
		Transform obj = UnityEngine.Object.Instantiate(categoryTemplate, categoryTemplate.parent);
		obj.GetLanguageChangeEventByName("Title").SetData(categoryLabel);
		obj.gameObject.SetActive(value: true);
		obj.name = categoryTemplate.name + " Clone";
	}

	private void SetLabel(LanguageChangeEventDataHolder text, Color color, FontStyles fontStyle, TextAlignmentOptions alignmentOptions)
	{
		Transform obj = UnityEngine.Object.Instantiate(labelTemplate, labelTemplate.parent);
		obj.GetComponent<TextLocalizationComponent>().SetData(text);
		TMP_Text component = obj.GetComponent<TMP_Text>();
		component.color = color;
		component.fontStyle = fontStyle;
		component.alignment = alignmentOptions;
		obj.gameObject.SetActive(value: true);
		obj.name = labelTemplate.name + " Clone";
	}

	private void SetLabel(string text, Color color, FontStyles fontStyle, TextAlignmentOptions alignmentOptions)
	{
		Transform obj = UnityEngine.Object.Instantiate(labelTemplate, labelTemplate.parent);
		TMP_Text component = obj.GetComponent<TMP_Text>();
		component.text = text;
		component.color = color;
		component.fontStyle = fontStyle;
		component.alignment = alignmentOptions;
		obj.gameObject.SetActive(value: true);
		obj.name = labelTemplate.name + " Clone";
	}

	private void SetProgressBar(int progress, Color32 color)
	{
		ProgressBar component = UnityEngine.Object.Instantiate(progressBarTemplate, progressBarTemplate.parent).GetComponent<ProgressBar>();
		component.autoSetColors = false;
		component.SetValue(progress);
		component.label.color = color;
		component.sliderFillImage.color = color;
		component.gameObject.SetActive(value: true);
		component.name = progressBarTemplate.name + " Clone";
	}

	private void SetList(List<Tuple<(string, Color), string>> elements)
	{
		Transform transform = UnityEngine.Object.Instantiate(keyValueListTemplate, keyValueListTemplate.parent);
		Transform transform2 = transform.Find("Entry");
		foreach (var (tuple3, text2) in elements)
		{
			Transform obj = UnityEngine.Object.Instantiate(transform2, transform2.parent);
			TextLocalizationComponent languageChangeEventByName = obj.GetLanguageChangeEventByName("Left");
			(languageChangeEventByName.Key, _) = tuple3;
			languageChangeEventByName.TextContainer.color = tuple3.Item2;
			obj.GetLabelByName("Right").text = text2;
			obj.gameObject.SetActive(value: true);
		}
		transform.name = keyValueListTemplate.name + " Clone";
		transform.gameObject.SetActive(value: true);
	}

	private void SetList(List<string> elements, Color color = default(Color))
	{
		Color color2 = ((color == default(Color)) ? Color.white : color);
		Transform transform = UnityEngine.Object.Instantiate(listTemplate, listTemplate.parent);
		Transform transform2 = transform.Find("Entry");
		foreach (string element in elements)
		{
			Transform obj = UnityEngine.Object.Instantiate(transform2, transform2.parent);
			TextLocalizationComponent component = obj.GetComponent<TextLocalizationComponent>();
			if (LocalizorManager.IsLocalizedKey(element))
			{
				component.Prefix = "- ";
				component.Key = element;
			}
			else
			{
				component.TextContainer.text = element;
			}
			component.TextContainer.color = color2;
			obj.gameObject.SetActive(value: true);
		}
		transform.name = listTemplate.name + " Clone";
		transform.gameObject.SetActive(value: true);
	}

	private void SetList(List<(string, object)> elements, Color color = default(Color))
	{
		Color color2 = ((color == default(Color)) ? Color.white : color);
		Transform transform = UnityEngine.Object.Instantiate(keyValueListTemplate, keyValueListTemplate.parent);
		Transform transform2 = transform.Find("Entry");
		foreach (var element in elements)
		{
			Transform obj = UnityEngine.Object.Instantiate(transform2, transform2.parent);
			TextLocalizationComponent languageChangeEventByName = obj.GetLanguageChangeEventByName("Left");
			(languageChangeEventByName.Key, languageChangeEventByName.Arguments) = element;
			languageChangeEventByName.TextContainer.color = color2;
			obj.GetLabelByName("Right").text = null;
			obj.gameObject.SetActive(value: true);
		}
		transform.name = keyValueListTemplate.name + " Clone";
		transform.gameObject.SetActive(value: true);
	}

	private void SetCheckboxList(List<Tuple<string, bool>> elements)
	{
		Transform transform = UnityEngine.Object.Instantiate(checkboxListTemplate, checkboxListTemplate.parent);
		Transform transform2 = transform.Find("Entry");
		foreach (var (key, isOn) in elements)
		{
			Transform obj = UnityEngine.Object.Instantiate(transform2, transform2.parent);
			obj.GetComponent<TextLocalizationComponent>().Key = key;
			obj.Find("Toggle").GetComponent<Toggle>().isOn = isOn;
			obj.gameObject.SetActive(value: true);
		}
		transform.name = checkboxListTemplate.name + " Clone";
		transform.gameObject.SetActive(value: true);
	}

	private void SetDemandsList(List<(string, (string, Color), bool)> elements)
	{
		Transform transform = UnityEngine.Object.Instantiate(demandsListTemplate, demandsListTemplate.parent);
		Transform transform2 = transform.Find("Entry");
		foreach (var element in elements)
		{
			string item = element.Item1;
			(string, Color) item2 = element.Item2;
			bool item3 = element.Item3;
			Transform obj = UnityEngine.Object.Instantiate(transform2, transform2.parent);
			obj.GetLanguageChangeEventByName("DemandName").Key = item;
			TextLocalizationComponent languageChangeEventByName = obj.GetLanguageChangeEventByName("DemandPriority");
			(languageChangeEventByName.Key, _) = item2;
			languageChangeEventByName.TextContainer.color = item2.Item2;
			obj.Find("Toggle").GetComponent<Toggle>().isOn = item3;
			obj.gameObject.SetActive(value: true);
		}
		transform.name = demandsListTemplate.name + " Clone";
		transform.gameObject.SetActive(value: true);
	}

	private void SetImage(Sprite newImage)
	{
		Image image = UnityEngine.Object.Instantiate(imageTemplate, imageTemplate.transform.parent);
		image.sprite = newImage;
		image.gameObject.SetActive(value: true);
		image.gameObject.name = imageTemplate.name + " Clone";
	}

	private void ResetData()
	{
		RemoveClones(splitterTemplate);
		RemoveClones(headerTemplate);
		RemoveClones(categoryTemplate);
		RemoveClones(labelTemplate);
		RemoveClones(progressBarTemplate);
		RemoveClones(keyValueListTemplate);
		RemoveClones(listTemplate);
		RemoveClones(checkboxListTemplate);
		RemoveClones(imageTemplate.transform);
		if (demandsListTemplate != null)
		{
			RemoveClones(demandsListTemplate);
		}
	}

	private static void RemoveClones(Transform template)
	{
		string text = template.name + " Clone";
		foreach (Transform item in template.parent)
		{
			if (item.name == text)
			{
				UnityEngine.Object.Destroy(item.gameObject);
			}
		}
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		IsVisible = false;
		IsPaused = false;
	}
}
