using System;
using System.Linq;
using Controllers;
using Helpers;
using Player.HUD.ItemWarningIcons;
using PlayerActivity;
using UI.Notification;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class EducationDoorController : ItemController
{
	[SerializeField]
	private StudyDiploma studyDiploma;

	public StudyDiploma StudyDiploma => studyDiploma;

	public override void Awake()
	{
		base.Awake();
		alwaysShowWarningIcon = true;
		itemWarningIconOffset = new Vector3(0f, 0.8f);
	}

	public override void Start()
	{
		base.Start();
		SetStudyDiploma();
		GameEvent.onGameEventTriggered = (Action<string>)Delegate.Combine(GameEvent.onGameEventTriggered, new Action<string>(OnGameEventTriggered));
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		GameEvent.onGameEventTriggered = (Action<string>)Delegate.Remove(GameEvent.onGameEventTriggered, new Action<string>(OnGameEventTriggered));
	}

	public override WarningIconType GetWarningIconType()
	{
		if (EducationHelper.HasCompletedDiploma(studyDiploma.diplomaName))
		{
			return WarningIconType.Completed;
		}
		DiplomaName requiredDiploma = EducationHelper.GetDiplomaData(StudyDiploma.diplomaName).requiredDiploma;
		if (requiredDiploma != DiplomaName.Undefined && !EducationHelper.HasCompletedDiploma(requiredDiploma))
		{
			return WarningIconType.None;
		}
		return WarningIconType.Action;
	}

	private void OnGameEventTriggered(string gameEvent)
	{
		if (gameEvent == "ba:gameevent_activityfinished")
		{
			InstanceBehavior<ItemWarningIconManager>.Instance.UpdateWarningIcon(this);
		}
	}

	private void SetStudyDiploma()
	{
		studyDiploma.exitPosition = navMeshTargets[0];
		if (!string.IsNullOrEmpty(customValue))
		{
			if (Enum.TryParse(typeof(DiplomaName), customValue, out var result))
			{
				studyDiploma.diplomaName = (DiplomaName)result;
				ItemController itemController = InstanceBehavior<BuildingManager>.Instance.allItemControllers.Where((ItemController x) => x is SeatController).FirstOrDefault((ItemController x) => x.customValue == studyDiploma.diplomaName.ToStringFast());
				if (itemController != null)
				{
					studyDiploma.chair = itemController.transform;
				}
				else
				{
					Debug.LogWarning($"Diploma chair '{studyDiploma.diplomaName}' not found");
				}
			}
			else
			{
				Debug.LogWarning("Diploma name '" + customValue + "' not found (" + base.gameObject.name + ")");
			}
		}
		else
		{
			Debug.LogWarning("Diploma name not set (" + base.gameObject.name + ")");
		}
	}

	public void StartLearning()
	{
		if (!InstanceBehavior<BuildingManager>.Instance.isOpen)
		{
			Notifications.ShowError("educationdoorcontroller_closed", "educationdoorcontroller_closed");
			return;
		}
		if ((PlayerHelper.ItemInstanceInHands != null && !PlayerHelper.IsHoldingBag) || !string.IsNullOrEmpty(SaveGameManager.Current.ActiveVehicleId))
		{
			Notifications.ShowError("businessschool_holding_item", "educationdoorcontroller_holding_item");
			return;
		}
		studyDiploma.computer = studyDiploma.chair.GetComponent<ItemController>().parentItemController;
		PlayerActivityUI.Show(studyDiploma, this);
	}
}
