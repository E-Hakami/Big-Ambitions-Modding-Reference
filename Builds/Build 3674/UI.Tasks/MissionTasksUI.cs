using System.Collections;
using Extensions;
using Helpers;
using JimmysUnityUtilities;
using Localizor.LanguageChangeEvent;
using Player.PlayerMissions;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Tasks;

public abstract class MissionTasksUI<TMission> where TMission : PlayerMission
{
	protected const string CheckmarkName = "Checkmark";

	protected const string DestinationButtonName = "DestinationButton";

	protected const string DestinationLabelPath = "Task/Label";

	protected const string ItemTemplatePath = "Task/Subtasks/SubLabel";

	private const string CloseButtonName = "CloseButton";

	private const string PreservedChildName = "UiTemplate";

	private const string TimeLabelName = "Label";

	private const float UpdateInterval = 0.3f;

	private static readonly WaitForSeconds WaitSeconds = new WaitForSeconds(0.3f);

	protected Transform tasksGroup;

	protected TextLocalizationComponent timeLabel;

	protected Color timeLabelDefaultColor;

	private string _highlight;

	private int _lastMinutesLeft;

	private Coroutine _updateCoroutine;

	public abstract void UpdateUI();

	public void StartUpdateRoutine()
	{
		if (SaveGameManager.Current.currentPlayerMission is TMission && _updateCoroutine == null)
		{
			_updateCoroutine = CoroutineUtility.Run(UpdateRoutine());
		}
	}

	public void Dispose()
	{
		CoroutineUtility.StopRunning(_updateCoroutine);
		_updateCoroutine = null;
	}

	public void Hide()
	{
		if ((bool)tasksGroup)
		{
			Dispose();
			OnHide();
			DestroyTasksGroup();
		}
	}

	protected abstract void OnClickCancelJob();

	protected virtual void OnHide()
	{
	}

	protected bool TryGetMission(out TMission mission)
	{
		if (SaveGameManager.Current?.currentPlayerMission is TMission val)
		{
			mission = val;
			return true;
		}
		mission = null;
		Hide();
		return false;
	}

	protected void CreateTasksGroup(string titleKey)
	{
		tasksGroup = InstanceBehavior<UIs>.Instance.tasksUI.SetUpTasksGroup(titleKey);
		tasksGroup.SetAsFirstSibling();
		tasksGroup.ClearChildren(keepHiddenChildren: false, "UiTemplate");
		_highlight = ColorUtility.ToHtmlStringRGB(InstanceBehavior<UIs>.Instance.tasksUI.highlightTextColor);
	}

	protected Transform CreateTimeEntry()
	{
		Transform transform = Object.Instantiate(InstanceBehavior<UIs>.Instance.tasksUI.taskEntryTemplate, tasksGroup);
		ButtonEffects component = transform.GetComponent<ButtonEffects>();
		if ((bool)component)
		{
			Object.Destroy(component);
		}
		timeLabel = transform.GetLanguageChangeEventByName("Label");
		timeLabel.SetValue(string.Empty);
		_lastMinutesLeft = -1;
		timeLabelDefaultColor = timeLabel.TextContainer.color;
		timeLabel.TextContainer.color = InstanceBehavior<UIs>.Instance.tasksUI.highlightTextColor;
		transform.Find("Checkmark").gameObject.SetActive(value: false);
		transform.Find("DestinationButton").gameObject.SetActive(value: false);
		Button component2 = transform.Find("CloseButton").GetComponent<Button>();
		component2.gameObject.SetActive(value: true);
		component2.onClick.AddListener(OnClickCancelJob);
		transform.gameObject.SetActive(value: true);
		return transform;
	}

	protected void UpdateTimeLabel(TMission mission)
	{
		int minutesLeft = mission.GetMinutesLeft();
		if (minutesLeft != _lastMinutesLeft)
		{
			_lastMinutesLeft = minutesLeft;
			timeLabel.SetValue(mission.GetTimeLeftFormatted());
		}
	}

	protected Transform CreateAddressEntry(string addressText, out TextLocalizationComponent addressLabel)
	{
		Transform transform = Object.Instantiate(InstanceBehavior<UIs>.Instance.tasksUI.nestedTaskEntryTemplate, tasksGroup);
		addressLabel = transform.GetLanguageChangeEventByName("Task/Label");
		addressLabel.SetValue(addressText, clearKey: true);
		addressLabel.TextContainer.richText = true;
		return transform;
	}

	protected static TextLocalizationComponent CreateItemSubLabel(Transform itemTemplate, string itemName, int targetAmount)
	{
		Transform transform = Object.Instantiate(itemTemplate, itemTemplate.parent);
		TextLocalizationComponent component = transform.GetComponent<TextLocalizationComponent>();
		component.SetData(LocalizationHelper.GetItemLabel(itemName, targetAmount));
		transform.gameObject.SetActive(value: true);
		return component;
	}

	protected string FormatAddressWithDistance(string addressText, string distanceText)
	{
		return addressText + " (<color=#" + _highlight + ">" + distanceText + "</color>)";
	}

	protected void DestroyTasksGroup()
	{
		if ((bool)tasksGroup)
		{
			Object.Destroy(tasksGroup.gameObject);
			InstanceBehavior<UIs>.Instance.tasksUI.ScheduleUpdateObjectivesHeight();
		}
		tasksGroup = null;
		timeLabel = null;
	}

	private IEnumerator UpdateRoutine()
	{
		while (SaveGameManager.Current?.currentPlayerMission is TMission)
		{
			yield return WaitSeconds;
			UpdateUI();
		}
		_updateCoroutine = null;
	}
}
