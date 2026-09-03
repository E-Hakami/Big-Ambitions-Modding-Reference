using System;
using System.Collections.Generic;
using System.Linq;
using Buildings.BuildingTypes.Special.PrivateDriverService;
using Entities;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Player.HUD.SmartphoneUI;
using UI;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SmartphoneUI : MonoBehaviour
{
	public static readonly UnityEvent<AppName> OnUpdatedBadgeCount = new UnityEvent<AppName>();

	public readonly Dictionary<AppName, SmartphoneAppButton> appButtons = new Dictionary<AppName, SmartphoneAppButton>();

	public RadioControls radioControls;

	[SerializeField]
	private SmartphoneApps apps;

	[SerializeField]
	private Transform appButtonTemplate;

	[SerializeField]
	private Button privateDriverButton;

	public SmartphonePrivateDriverUI privateDriverUI;

	private bool _isThereAnAppOutlined;

	private AppName _currentOutlinedApp;

	private SmartPhoneFrame _smartPhoneFrame;

	[HideInInspector]
	public int appOutlinedIndex = -1;

	private void Awake()
	{
		_smartPhoneFrame = GetComponent<SmartPhoneFrame>();
		appButtonTemplate.ResetTemplate();
		SmartphoneApp[] appList = apps.appList;
		foreach (SmartphoneApp app in appList)
		{
			Transform obj = appButtonTemplate.CreateElement();
			obj.name = app.appName.ToStringFast();
			obj.GetComponent<Button>().onClick.AddListener(delegate
			{
				OpenApp(app.appName);
			});
			SmartphoneAppButton component = obj.GetComponent<SmartphoneAppButton>();
			component.SetTitle(app.appName);
			component.SetIcon(app.icon);
			appButtons.Add(app.appName, component);
		}
		UpdateBadgeCount(AppName.Contacts, playSound: false);
		UpdateBadgeCount(AppName.MyEmployees, playSound: false);
		GlobalEvents.onFullMenuToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onFullMenuToggle, (Action<bool>)delegate(bool show)
		{
			if (!CityMap.IsOpen)
			{
				base.gameObject.SetActive(!show && !GameManager.IsAnyMiniGameActive());
			}
		});
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool show)
		{
			base.gameObject.SetActive(!show && !GameManager.IsAnyMiniGameActive());
		});
		privateDriverButton.onClick.AddListener(delegate
		{
			privateDriverUI.OnClickPrivateDriverButton();
		});
		InstanceBehavior<UIs>.Instance.smartphoneCollapsibleWindow.onStateChange.AddListener(delegate
		{
			UpdatePrivateDriverEnabled();
		});
		GlobalEvents.RegisterOnGameLoadedCallback(UpdatePrivateDriverEnabled);
	}

	public void UpdatePrivateDriverEnabled()
	{
		PrivateDriverContract activeContract = PrivateDriverHelpers.GetActiveContract();
		bool flag = (bool)activeContract && !InstanceBehavior<UIs>.Instance.smartphoneCollapsibleWindow.IsCollapsed;
		privateDriverButton.gameObject.SetActive(flag);
		if (!flag)
		{
			privateDriverUI.gameObject.SetActive(value: false);
		}
		if (!activeContract)
		{
			InstanceBehavior<UIs>.Instance.smartphoneUI.DismissPrivateDriver();
		}
	}

	public void DismissPrivateDriver(bool force = false, VehicleInstance vehicleInstance = null, bool instantRemove = false)
	{
		privateDriverUI.DismissPrivateDriver(force, vehicleInstance, instantRemove);
	}

	public void RebuildPrivateDriverUI()
	{
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			if ((bool)this && (bool)privateDriverUI)
			{
				privateDriverUI.RebuildUI();
			}
		});
	}

	public void UpdateFrame()
	{
		_smartPhoneFrame.UpdateFrame();
	}

	public void OpenApp(string appName)
	{
		Enum.TryParse<AppName>(appName, out var result);
		OpenApp(result);
	}

	public void OpenApp(AppName appName)
	{
		if (_isThereAnAppOutlined)
		{
			appButtons[_currentOutlinedApp].HideOutline();
			_isThereAnAppOutlined = false;
		}
		InstanceBehavior<UIs>.Instance.buildingResume.Close();
		if (appName == AppName.VoogleMaps)
		{
			if (CityMap.CanOpenMap())
			{
				InstanceBehavior<CityManager>.Instance.cityMap.Toggle();
			}
		}
		else
		{
			InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(appName);
		}
	}

	public void StartButtonSelection()
	{
		appOutlinedIndex = 0;
		OutlineApp((AppName)appOutlinedIndex);
	}

	public void OutlineLeftApp()
	{
		appOutlinedIndex--;
		if (appOutlinedIndex < 0)
		{
			appOutlinedIndex = appButtons.Count - 1;
		}
		OutlineApp((AppName)appOutlinedIndex);
	}

	public void OutlineRightApp()
	{
		appOutlinedIndex++;
		if (appOutlinedIndex >= appButtons.Count)
		{
			appOutlinedIndex = 0;
		}
		OutlineApp((AppName)appOutlinedIndex);
	}

	public void OutlineDownApp()
	{
		if (appOutlinedIndex == appButtons.Count - 1)
		{
			appOutlinedIndex = 0;
		}
		else
		{
			appOutlinedIndex += 2;
			if (appOutlinedIndex >= appButtons.Count)
			{
				appOutlinedIndex = appButtons.Count - 1;
			}
		}
		OutlineApp((AppName)appOutlinedIndex);
	}

	public void OutlineUpApp()
	{
		if (appOutlinedIndex == 0)
		{
			appOutlinedIndex = appButtons.Count - 1;
		}
		else
		{
			appOutlinedIndex -= 2;
			if (appOutlinedIndex < 0)
			{
				appOutlinedIndex = 0;
			}
		}
		OutlineApp((AppName)appOutlinedIndex);
	}

	public void OutlineApp(AppName appName)
	{
		if (_isThereAnAppOutlined)
		{
			appButtons[_currentOutlinedApp].HideOutline();
		}
		appButtons[appName].ShowOutline();
		_currentOutlinedApp = appName;
		_isThereAnAppOutlined = true;
	}

	public void StopOutliningCurrentApp()
	{
		if (_isThereAnAppOutlined)
		{
			appButtons[_currentOutlinedApp].HideOutline();
			_isThereAnAppOutlined = false;
		}
	}

	public void OpenOutlinedApp()
	{
		if (_isThereAnAppOutlined)
		{
			OpenApp(_currentOutlinedApp);
		}
	}

	public void UpdateBadgeCount(AppName appName, bool playSound = true)
	{
		int num = appName switch
		{
			AppName.Contacts => SaveGameManager.Current.Contacts.Sum((Contact contact) => contact.NumberOfUnreadMessages), 
			AppName.MyEmployees => SaveGameManager.Current.CandidateEmployeeInstances.Count, 
			_ => 0, 
		};
		if (playSound && num > 0 && appName == AppName.Contacts)
		{
			UiSoundHelper.Play(UiSound.NotificationMessage);
		}
		if (appButtons.TryGetValue(appName, out var value))
		{
			value.UpdateBadgeCount(num);
		}
		OnUpdatedBadgeCount?.Invoke(appName);
	}
}
