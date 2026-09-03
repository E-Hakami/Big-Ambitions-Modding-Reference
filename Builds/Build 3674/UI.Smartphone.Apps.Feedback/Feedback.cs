using System;
using System.Collections;
using System.IO;
using System.Reflection;
using System.Text;
using Extensions;
using Localizor;
using TMPro;
using UI.Components;
using UI.Notification;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace UI.Smartphone.Apps.Feedback;

public class Feedback : InstanceBehavior<Feedback>
{
	private static readonly string[] IgnoredSystemInformation = new string[8] { "deviceName", "deviceModel", "deviceUniqueIdentifier", "graphicsDeviceID", "graphicsDeviceVendorID", "operatingSystemFamily", "graphicsDeviceType", "" };

	private static readonly IFeedbackData[] FeedbackDataProviders = new IFeedbackData[3]
	{
		new LayoutFeedbackData(),
		new ScreenshotFeedbackData(),
		new SavegameFeedbackData()
	};

	[SerializeField]
	private GameObject container;

	[SerializeField]
	private Button submitButton;

	[SerializeField]
	private TMP_InputField inputField;

	[SerializeField]
	private Toggle systemDataCollectionToggle;

	[SerializeField]
	private GameObject dimmer;

	[SerializeField]
	private Texture2D cursorTexture;

	private int _sendTimeout;

	private bool _wasGameSpeedDisabled;

	private bool _isUsingWeMod;

	private bool _isUsingMelonLoader;

	public static bool IsOpen { get; private set; }

	public void Start()
	{
		container.gameObject.SetActive(value: false);
		dimmer.SetActive(value: false);
		_sendTimeout = 20;
		ScreenshotFeedbackData.CursorTexture = cursorTexture;
	}

	public void OpenWithScreenshot()
	{
		StartCoroutine(OpenFeedbackDelayed());
	}

	public void Toggle()
	{
		if (IsOpen)
		{
			Toggle(show: false);
		}
		else if (!SaveGameManager.IsModdedSave)
		{
			OpenWithScreenshot();
		}
	}

	public void Toggle(bool show)
	{
		container.gameObject.SetActive(show);
		dimmer.SetActive(show);
		IsOpen = show;
		if (!FullMenu.IsOpen)
		{
			InstanceBehavior<SfxManager>.Instance.SetSoundSnapshotCityMap(show, 0.2f);
		}
		if (show)
		{
			KeyboardInputHelper.FocusNextFrame(inputField);
		}
		if (!InstanceBehavior<UIs>.Instance)
		{
			return;
		}
		if (show)
		{
			if (InstanceBehavior<UIs>.Instance.gameSpeed.isTimeControlDisabled)
			{
				_wasGameSpeedDisabled = true;
				return;
			}
			InstanceBehavior<UIs>.Instance.gameSpeed.SetPause(newPause: true, showOverlay: false);
			InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: true);
			_wasGameSpeedDisabled = false;
		}
		else if (!_wasGameSpeedDisabled)
		{
			InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: false);
			InstanceBehavior<UIs>.Instance.gameSpeed.Reset();
		}
	}

	private IEnumerator OpenFeedbackDelayed()
	{
		IFeedbackData[] feedbackDataProviders = FeedbackDataProviders;
		for (int i = 0; i < feedbackDataProviders.Length; i++)
		{
			feedbackDataProviders[i].GatherData();
		}
		_isUsingWeMod = IsUsingWeMod();
		_isUsingMelonLoader = IsUsingMelonLoader();
		yield return new WaitForEndOfFrame();
		feedbackDataProviders = FeedbackDataProviders;
		for (int i = 0; i < feedbackDataProviders.Length; i++)
		{
			feedbackDataProviders[i].GatherDataDelayed();
		}
		yield return null;
		Toggle(show: true);
	}

	public void SubmitFeedback()
	{
		if (!string.IsNullOrEmpty(inputField.text))
		{
			submitButton.interactable = false;
			inputField.interactable = false;
			systemDataCollectionToggle.interactable = false;
			StartCoroutine(Send());
		}
	}

	private IEnumerator Send()
	{
		WWWForm formData = new WWWForm();
		formData.AddField("gameVersion", GameVersion.GetCurrent().GetFullVersionString());
		formData.AddField("computerId", GetDeviceUid());
		formData.AddField("description", inputField.text);
		if (systemDataCollectionToggle.isOn)
		{
			string allLogs = DebugLogCollector.GetAllLogs();
			formData.AddBinaryData("log", CompressString(allLogs), "log.log", "text/plain");
			formData.AddBinaryData("sysinfo", CompressString(CollectSystemInformation()), "sysinfo.txt", "text/csv");
			IFeedbackData[] feedbackDataProviders = FeedbackDataProviders;
			for (int i = 0; i < feedbackDataProviders.Length; i++)
			{
				feedbackDataProviders[i].AddToForm(ref formData);
			}
		}
		using UnityWebRequest www = UnityWebRequest.Post("https://gametools.hovgaard.com/", formData);
		www.SetRequestHeader("Accept", "application/json");
		www.SetRequestHeader("X-API-Version", "2");
		www.timeout = _sendTimeout;
		Toggle(show: false);
		UnityWebRequestAsyncOperation result = www.SendWebRequest();
		yield return result;
		string text = Encoding.UTF8.GetString(www.downloadHandler.data);
		submitButton.interactable = true;
		inputField.interactable = true;
		systemDataCollectionToggle.interactable = true;
		if (result.webRequest.responseCode == 200)
		{
			Notifications.Show(NotificationType.Success, "feedback_notification_success");
			inputField.text = null;
			Toggle(show: false);
		}
		else
		{
			Notifications.Show(NotificationType.Error, "feedback_notification_failure");
			Debug.LogError("Failed to send feedback: " + text);
			_sendTimeout *= 2;
		}
	}

	private string CollectSystemInformation()
	{
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("GameVersion;" + GameVersion.GetCurrent().GetFullVersionString());
		stringBuilder.AppendLine("LoadedLocale;" + LocalizorManager.LoadedLocale);
		stringBuilder.AppendLine("DeviceUID;" + GetDeviceUid());
		string[] joystickNames = Input.GetJoystickNames();
		stringBuilder.AppendLine($"AttachedControllersCount;{joystickNames.Length}");
		for (int i = 0; i < joystickNames.Length; i++)
		{
			stringBuilder.AppendLine($"Joystick{i};{joystickNames[i]}");
		}
		stringBuilder.AppendLine($"uiZooming;{PlayerPrefSettings.uiZooming}");
		stringBuilder.AppendLine($"Fullscreen;{Screen.fullScreen}");
		stringBuilder.AppendLine($"Resolution;{Screen.currentResolution.width}x{Screen.currentResolution.height}");
		stringBuilder.AppendLine($"WeMod;{_isUsingWeMod}");
		stringBuilder.AppendLine($"MelonLoader;{_isUsingMelonLoader}");
		PropertyInfo[] properties = typeof(SystemInfo).GetProperties();
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (!IsIgnoredSystemInformation(propertyInfo.Name))
			{
				stringBuilder.AppendLine($"{propertyInfo.Name};{propertyInfo.GetValue(null)}");
			}
		}
		return stringBuilder.ToString();
	}

	private static byte[] CompressString(string log)
	{
		return SaveGameSerializationHelper.CompressBytes(Encoding.UTF8.GetBytes(log));
	}

	private static string GetDeviceUid()
	{
		string text = SystemInfo.deviceUniqueIdentifier;
		if (text == "n/a")
		{
			text = UnityEngine.PlayerPrefs.GetString("deviceUniqueIdentifier", "");
			if (text == "")
			{
				text = Guid.NewGuid().ToString();
				UnityEngine.PlayerPrefs.SetString("deviceUniqueIdentifier", text);
			}
		}
		return text;
	}

	private static bool IsUsingWeMod()
	{
		return GenericExtensions.IsProgramOpen("wemod");
	}

	private static bool IsUsingMelonLoader()
	{
		return Directory.Exists(Path.GetFullPath("./MelonLoader"));
	}

	private static bool IsIgnoredSystemInformation(string propertyName)
	{
		for (int i = 0; i < IgnoredSystemInformation.Length; i++)
		{
			if (string.Equals(IgnoredSystemInformation[i], propertyName, StringComparison.OrdinalIgnoreCase))
			{
				return true;
			}
		}
		return false;
	}
}
