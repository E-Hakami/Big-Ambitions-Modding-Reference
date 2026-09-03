using System;
using Entities;
using Localizor.LanguageChangeEvent;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.Contacts;

[RequireComponent(typeof(Button))]
public class ContextButton : MonoBehaviour
{
	public enum BackgroundColor
	{
		gray,
		red,
		green,
		blue,
		orange
	}

	[SerializeField]
	private TextLocalizationComponent localizationComponent;

	[SerializeField]
	private Image backgroundImage;

	[Header("Backgrounds")]
	[SerializeField]
	private Sprite grayBackground;

	[SerializeField]
	private Sprite redBackground;

	[SerializeField]
	private Sprite greenBackground;

	[SerializeField]
	private Sprite blueBackground;

	[SerializeField]
	private Sprite orangeBackground;

	[HideInInspector]
	public string groupId;

	private Button _button;

	public void SetUp(string groupId, TextMessage.ContextButtonData buttonData, AdditionalMessageData messageData)
	{
		this.groupId = groupId;
		localizationComponent.Key = buttonData.key;
		Image image = backgroundImage;
		image.sprite = buttonData.backgroundColor switch
		{
			BackgroundColor.gray => grayBackground, 
			BackgroundColor.red => redBackground, 
			BackgroundColor.green => greenBackground, 
			BackgroundColor.blue => blueBackground, 
			BackgroundColor.orange => orangeBackground, 
			_ => throw new ArgumentOutOfRangeException("backgroundColor", buttonData.backgroundColor, null), 
		};
		if (buttonData.onClick == null)
		{
			return;
		}
		if ((object)_button == null)
		{
			_button = GetComponent<Button>();
		}
		_button.onClick.RemoveAllListeners();
		_button.onClick.AddListener(delegate
		{
			if (_button.interactable)
			{
				_button.interactable = false;
				buttonData.onClick();
				messageData?.contextButtonData?.Clear();
				ContactsApp.SetContextButtonsNoninteractable(groupId);
			}
		});
	}

	public void SetUp(string localizationKey, BackgroundColor backgroundColor, Action onClick)
	{
		base.name = localizationKey;
		localizationComponent.Key = localizationKey;
		Image image = backgroundImage;
		image.sprite = backgroundColor switch
		{
			BackgroundColor.gray => grayBackground, 
			BackgroundColor.red => redBackground, 
			BackgroundColor.green => greenBackground, 
			BackgroundColor.blue => blueBackground, 
			BackgroundColor.orange => orangeBackground, 
			_ => throw new ArgumentOutOfRangeException("backgroundColor", backgroundColor, null), 
		};
		if (onClick != null)
		{
			if ((object)_button == null)
			{
				_button = GetComponent<Button>();
			}
			_button.onClick.RemoveAllListeners();
			_button.onClick.AddListener(onClick.Invoke);
		}
	}

	public void SetUp(LanguageChangeEventDataHolder localization, BackgroundColor backgroundColor, Action onClick)
	{
		base.name = localization.Key;
		localizationComponent.SetData(localization);
		Image image = backgroundImage;
		image.sprite = backgroundColor switch
		{
			BackgroundColor.gray => grayBackground, 
			BackgroundColor.red => redBackground, 
			BackgroundColor.green => greenBackground, 
			BackgroundColor.blue => blueBackground, 
			BackgroundColor.orange => orangeBackground, 
			_ => throw new ArgumentOutOfRangeException("backgroundColor", backgroundColor, null), 
		};
		if (onClick != null)
		{
			if ((object)_button == null)
			{
				_button = GetComponent<Button>();
			}
			_button.onClick.RemoveAllListeners();
			_button.onClick.AddListener(onClick.Invoke);
		}
	}

	public void SetInteractable(bool interactable)
	{
		_button.interactable = interactable;
	}
}
