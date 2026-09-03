using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Dialogs;
using Entities;
using Extensions;
using JimmysUnityUtilities;
using Localizor;
using Localizor.LanguageChangeEvent;
using TMPro;
using UI.Components;
using UI.Elements;
using UI.Smartphone.Apps.Contacts;
using UnityEngine;
using UnityEngine.UI;

public class DialogController : MonoBehaviour
{
	private const int MaxNumberOfDialogs = 30;

	public static DialogController current;

	public DialogType dialogType;

	[SerializeField]
	private ContextButton buttonPrefab;

	[SerializeField]
	protected CanvasGroup inputTemplate;

	[SerializeField]
	protected CanvasGroup textTemplate;

	[SerializeField]
	protected ScrollRect scrollRect;

	public Dialog dialog;

	[NonSerialized]
	public Contact contact;

	private DialogEntry _currentEntry;

	private CanvasGroup _currentEntryComponent;

	private Transform _currentInputTemplate;

	private int numberOfDialogs;

	public void ShowEntry(DialogEntry entry)
	{
		DisableCurrentEntryInteraction();
		if (AddNewEntryComponent(entry))
		{
			LimitNumberOfDialogs();
			ResetScrollPosition();
			TweenerCore<float, float, FloatOptions> tweenerCore = _currentEntryComponent.DOFade(1f, 1f).SetLink(_currentEntryComponent.gameObject).SetUpdate(isIndependentUpdate: true);
			tweenerCore.onComplete = (TweenCallback)Delegate.Combine(tweenerCore.onComplete, (TweenCallback)delegate
			{
				entry.OnVisible?.Invoke();
			});
			Button upKeyboardInput = SetCurrentEntryButtons();
			SetUpKeyboardInput(upKeyboardInput);
		}
	}

	private bool AddNewEntryComponent(DialogEntry newEntry)
	{
		switch (newEntry.Template)
		{
		case DialogEntry.TemplateType.Input:
			SetCurrentEntry(inputTemplate);
			SetActiveInputTemplate(newEntry.InputTemplate);
			break;
		case DialogEntry.TemplateType.Text:
			SetCurrentEntry(textTemplate);
			SetComponentData(newEntry.messageData, newEntry.Icon, newEntry.SecondaryIcon);
			break;
		default:
			return false;
		}
		Transform transform = _currentEntryComponent.transform.Find("HeaderLabel");
		if (transform != null)
		{
			if (string.IsNullOrEmpty(newEntry.headerKey))
			{
				transform.gameObject.SetActive(value: false);
			}
			else
			{
				transform.GetComponent<TextLocalizationComponent>().Key = newEntry.headerKey;
			}
		}
		_currentEntryComponent.gameObject.name = (string.IsNullOrEmpty(newEntry.headerKey) ? newEntry.messageData.Key : newEntry.headerKey);
		_currentEntryComponent.gameObject.SetActive(value: true);
		_currentEntry = newEntry;
		numberOfDialogs++;
		return true;
	}

	private Button SetCurrentEntryButtons()
	{
		bool active = false;
		Button result = null;
		Transform transform = _currentEntryComponent.transform.Find("Buttons");
		transform.DestroyAllChildren();
		if (_currentEntry.OnCancel != null)
		{
			ContextButton contextButton = UnityEngine.Object.Instantiate(buttonPrefab, transform);
			string localizationKey = ((dialogType == DialogType.PhoneCall) ? "dialog_hang_up_button" : "itempanelui_buttons_leave");
			if (_currentEntry.CancelTextOverride != null)
			{
				localizationKey = _currentEntry.CancelTextOverride;
			}
			contextButton.SetUp(localizationKey, ContextButton.BackgroundColor.gray, _currentEntry.OnCancel);
			active = true;
		}
		if (_currentEntry.OnSecondOption != null)
		{
			UnityEngine.Object.Instantiate(buttonPrefab, transform).SetUp(_currentEntry.SecondOptionTextOverride, ContextButton.BackgroundColor.orange, SecondOptionCurrentEntry);
			active = true;
		}
		if (_currentEntry.OnConfirm != null)
		{
			ContextButton contextButton2 = UnityEngine.Object.Instantiate(buttonPrefab, transform);
			contextButton2.SetUp((!string.IsNullOrEmpty(_currentEntry.ConfirmTextOverride.Key)) ? _currentEntry.ConfirmTextOverride : "common_confirm".Localize(), ContextButton.BackgroundColor.blue, ConfirmCurrentEntry);
			result = contextButton2.GetComponent<Button>();
			active = true;
		}
		transform.gameObject.SetActive(active);
		LayoutRebuilder.ForceRebuildLayoutImmediate(transform.GetComponent<RectTransform>());
		ResetScrollPosition();
		CoroutineUtility.RunAfterOneFrame(ResetScrollPosition);
		return result;
	}

	private void SetUpKeyboardInput(Button confirmButton)
	{
		if (!confirmButton)
		{
			return;
		}
		if (_currentEntry.Template != DialogEntry.TemplateType.Input)
		{
			KeyboardInputHelper.SelectNextFrame(confirmButton);
			return;
		}
		TMP_InputField inputField = null;
		int num = 0;
		TMP_InputField[] componentsInChildren = _currentInputTemplate.GetComponentsInChildren<TMP_InputField>(includeInactive: false);
		foreach (TMP_InputField tMP_InputField in componentsInChildren)
		{
			if (!tMP_InputField.GetComponentInParent<UI.Elements.Dropdown>(includeInactive: true))
			{
				inputField = tMP_InputField;
				num++;
			}
		}
		switch (num)
		{
		case 1:
			KeyboardInputHelper.Configure(inputField, ConfirmCurrentEntry);
			break;
		case 0:
			KeyboardInputHelper.SelectNextFrame(confirmButton);
			break;
		}
	}

	private void SetCurrentEntry(CanvasGroup newEntryComponent)
	{
		_currentEntryComponent = UnityEngine.Object.Instantiate(newEntryComponent, newEntryComponent.transform.parent);
	}

	private void SetComponentData(LanguageChangeEventDataHolder entryData, Sprite background, Sprite facialExpression)
	{
		if (facialExpression != null)
		{
			Transform obj = _currentEntryComponent.transform.Find("Message/Mood");
			obj.Find("MoodBackground").GetComponent<Image>().sprite = background;
			obj.Find("FacialExpression").GetComponent<Image>().sprite = facialExpression;
			obj.gameObject.SetActive(value: true);
		}
		TextLocalizationComponent languageChangeEventByName = _currentEntryComponent.transform.Find("Message").GetLanguageChangeEventByName("MessageText");
		_currentEntryComponent.transform.Find("SpecialMessage")?.gameObject.SetActive(value: false);
		languageChangeEventByName.SetData(entryData);
	}

	public void ConfirmCurrentEntry()
	{
		DialogEntry dialogEntry = _currentEntry.OnConfirm();
		if (dialogEntry != null)
		{
			ShowEntry(dialogEntry);
		}
	}

	public void SecondOptionCurrentEntry()
	{
		DialogEntry dialogEntry = _currentEntry.OnSecondOption();
		if (dialogEntry != null)
		{
			ShowEntry(dialogEntry);
		}
	}

	public void OnCancel()
	{
		_currentEntry.OnCancel?.Invoke();
	}

	public virtual void FinishDialog()
	{
		DisableCurrentEntryInteraction();
	}

	public virtual void CancelDialog()
	{
		if (dialog != null && _currentEntry.onCancelMessage != null)
		{
			contact?.ReceivePlayerMessage(_currentEntry.onCancelMessage);
		}
		DisableCurrentEntryInteraction();
	}

	public void ForceCancel()
	{
		_currentEntry.OnForcedCancel?.Invoke();
	}

	private void DisableCurrentEntryInteraction()
	{
		if (_currentEntry != null)
		{
			Transform transform = _currentEntryComponent.transform.Find("InputTemplates");
			if (transform != null)
			{
				transform.GetComponent<CanvasGroup>().interactable = false;
			}
			Button[] componentsInChildren = _currentEntryComponent.transform.Find("Buttons").GetComponentsInChildren<Button>();
			for (int i = 0; i < componentsInChildren.Length; i++)
			{
				componentsInChildren[i].interactable = false;
			}
		}
	}

	protected void CancelMessageFading()
	{
		if (_currentEntry != null)
		{
			_currentEntryComponent.DOKill();
		}
	}

	private void SetActiveInputTemplate(DialogEntry.InputTemplateName templateName)
	{
		foreach (Transform item in _currentEntryComponent.transform.Find("InputTemplates").transform)
		{
			bool flag = item.name == templateName.ToStringFast();
			item.gameObject.SetActive(flag);
			if (flag)
			{
				_currentInputTemplate = item;
			}
		}
	}

	private void ResetScrollPosition()
	{
		Canvas.ForceUpdateCanvases();
		LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
		scrollRect.verticalNormalizedPosition = 0f;
		Canvas.ForceUpdateCanvases();
	}

	public void ScrollConversationToBottom()
	{
		Canvas.ForceUpdateCanvases();
		scrollRect.normalizedPosition = new Vector2(0f, 0f);
		Canvas.ForceUpdateCanvases();
	}

	public void ResetEntriesComponents()
	{
		foreach (Transform item in textTemplate.transform.parent)
		{
			if (item.gameObject.activeSelf)
			{
				UnityEngine.Object.Destroy(item.gameObject);
			}
		}
		_currentEntry = null;
		_currentEntryComponent = null;
		_currentInputTemplate = null;
		numberOfDialogs = 0;
	}

	private void LimitNumberOfDialogs()
	{
		if (numberOfDialogs >= 30)
		{
			DeleteOldestEntry();
		}
	}

	private void DeleteOldestEntry()
	{
		foreach (Transform item in textTemplate.transform.parent)
		{
			if (item.gameObject.activeSelf)
			{
				UnityEngine.Object.Destroy(item.gameObject);
				break;
			}
		}
	}

	public T GetInputTransform<T>(string objectName)
	{
		return (string.IsNullOrWhiteSpace(objectName) ? _currentInputTemplate : _currentInputTemplate.Find(objectName)).GetComponent<T>();
	}

	public T GetInputTransformSafe<T>()
	{
		if (!_currentInputTemplate)
		{
			return default(T);
		}
		return GetInputTransform<T>(null);
	}

	public T GetInputComponent<T>()
	{
		return _currentInputTemplate.GetComponent<T>();
	}

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		current = null;
	}
}
