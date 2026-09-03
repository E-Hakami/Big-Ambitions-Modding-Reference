using System;
using System.Linq;
using BigAmbitions.InputSystem;
using Extensions;
using Localizor;
using Localizor.LanguageChangeEvent;
using UI.Elements;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Overlays;

public class OverlayUI : MonoBehaviour
{
	[Header("UI")]
	[SerializeField]
	private CanvasGroup canvasGroup;

	[Header("Header")]
	[SerializeField]
	private GameObject headerGameObject;

	[SerializeField]
	private GameObject firstLineGameObject;

	[SerializeField]
	private GameObject secondLineGameObject;

	[SerializeField]
	private TextLocalizationComponent firstLineLabel;

	[SerializeField]
	private TextLocalizationComponent secondLineLeftLabel;

	[SerializeField]
	private TextLocalizationComponent secondLineRightLabel;

	[Header("Buttons")]
	[SerializeField]
	private GameObject buttonsGameObject;

	[SerializeField]
	private Button buttonTemplate;

	public BuildingEntranceOverlay buildingEntrance = new BuildingEntranceOverlay();

	public GasStationOverlay gasStation = new GasStationOverlay();

	public ElevatorOverlay elevator = new ElevatorOverlay();

	private IOverlay _currentOverlay;

	public Type GetCurrentOverlayType => _currentOverlay?.GetType();

	public static bool IsVisible { get; private set; }

	public static void Show(IOverlay overlay)
	{
		InstanceBehavior<UIs>.Instance.overlayUI.ShowOverlay(overlay);
	}

	public static void Hide(IOverlay overlay)
	{
		InstanceBehavior<UIs>.Instance.overlayUI.HideOverlay(overlay);
	}

	public void RefreshButtons()
	{
		if (IsVisible && _currentOverlay != null)
		{
			SetUpButtons();
		}
	}

	private void ShowOverlay(IOverlay overlay)
	{
		_currentOverlay = overlay;
		IsVisible = true;
		UpdateDisplay();
		base.gameObject.SetActive(value: true);
	}

	private void HideOverlay(IOverlay overlay)
	{
		if (_currentOverlay == overlay)
		{
			HideCurrentOverlay();
		}
	}

	private void HideCurrentOverlay()
	{
		_currentOverlay = null;
		IsVisible = false;
		base.gameObject.SetActive(value: false);
	}

	private void Update()
	{
		if (_currentOverlay != null)
		{
			base.transform.position = GameManager.GetMainCamera().WorldToScreenPoint(_currentOverlay.GetTargetPosition());
		}
	}

	private void Awake()
	{
		GlobalEvents.onBindingsChanged = (Action)Delegate.Combine(GlobalEvents.onBindingsChanged, new Action(UpdateDisplay));
		GlobalEvents.onCityMapToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onCityMapToggle, (Action<bool>)delegate(bool toggled)
		{
			ChangeVisibility(!toggled);
		});
		GlobalEvents.onEnterVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onEnterVehicle, (Action<VehicleController>)delegate
		{
			UpdateDisplay();
		});
		GlobalEvents.onExitVehicle = (Action<VehicleController>)Delegate.Combine(GlobalEvents.onExitVehicle, (Action<VehicleController>)delegate
		{
			UpdateDisplay();
		});
		GlobalEvents.onHospitalRespawnStarts = (Action)Delegate.Combine(GlobalEvents.onHospitalRespawnStarts, new Action(HideCurrentOverlay));
		GlobalEvents.onVehicleVariablesChanged = (Action)Delegate.Combine(GlobalEvents.onVehicleVariablesChanged, new Action(UpdateDisplay));
		GlobalEvents.onFullMenuToggle = (Action<bool>)Delegate.Combine(GlobalEvents.onFullMenuToggle, (Action<bool>)delegate(bool isOn)
		{
			if (!isOn)
			{
				UpdateDisplay();
			}
		});
	}

	public void OnPlayerActionPressed(PlayerAction playerAction)
	{
		if (IsVisible)
		{
			ButtonInfo buttonInfo = _currentOverlay.GetButtons()?.FirstOrDefault((ButtonInfo x) => x.playerAction == playerAction);
			if (buttonInfo != null && buttonInfo.interactable)
			{
				playerAction.Reset();
				buttonInfo.onClick();
			}
		}
	}

	private void UpdateDisplay()
	{
		if (IsVisible)
		{
			SetUpHeader();
			SetUpButtons();
		}
	}

	private void SetUpHeader()
	{
		LabelInfo labelInfo = _currentOverlay.GetFirstLineLabel();
		LabelInfo labelInfo2 = _currentOverlay.GetSecondLineLeftLabel();
		LabelInfo labelInfo3 = _currentOverlay.GetSecondLineRightLabel();
		bool flag = labelInfo != null || labelInfo3 != null;
		if (!flag && labelInfo2 == null)
		{
			headerGameObject.SetActive(value: false);
			return;
		}
		SetUpHeaderLabel(firstLineLabel, labelInfo);
		if (labelInfo2 == null && labelInfo3 == null)
		{
			secondLineGameObject.SetActive(value: false);
		}
		else
		{
			SetUpHeaderLabel(secondLineLeftLabel, labelInfo2);
			SetUpHeaderLabel(secondLineRightLabel, labelInfo3);
			secondLineGameObject.SetActive(value: true);
		}
		firstLineGameObject.SetActive(flag);
		headerGameObject.SetActive(value: true);
	}

	private void SetUpHeaderLabel(TextLocalizationComponent label, LabelInfo labelInfo)
	{
		label.gameObject.SetActive(labelInfo != null);
		if (labelInfo != null)
		{
			if (labelInfo.localize)
			{
				label.SetData(labelInfo.key.Localize(labelInfo.arguments));
			}
			else
			{
				label.Key = "";
				label.SetValue(labelInfo.key);
			}
			label.TextContainer.color = labelInfo.color;
		}
	}

	private void SetUpButtons()
	{
		buttonTemplate.transform.ResetTemplate();
		ButtonInfo[] buttons = _currentOverlay.GetButtons();
		buttonsGameObject.SetActive(buttons != null);
		if (buttons != null)
		{
			ButtonInfo[] array = buttons;
			foreach (ButtonInfo button in array)
			{
				SetButton(button);
			}
		}
	}

	private void SetButton(ButtonInfo buttonInfo)
	{
		Button button = UnityEngine.Object.Instantiate(buttonTemplate, buttonTemplate.transform.parent);
		TextLocalizationComponent componentInChildren = button.GetComponentInChildren<TextLocalizationComponent>();
		componentInChildren.Key = buttonInfo.key;
		componentInChildren.Arguments = buttonInfo.arguments;
		componentInChildren.Suffix = buttonInfo.playerAction.AsSuffix();
		button.transform.Find("Container").GetComponent<Image>().sprite = GlobalReferences.GetButtonImageByName(buttonInfo.color);
		button.onClick.AddListener(delegate
		{
			if (buttonInfo.interactable)
			{
				buttonInfo.onClick();
			}
		});
		button.interactable = buttonInfo.interactable;
		button.gameObject.SetActive(value: true);
	}

	private void ChangeVisibility(bool show)
	{
		canvasGroup.alpha = (show ? 1 : 0);
		canvasGroup.blocksRaycasts = show;
	}
}
