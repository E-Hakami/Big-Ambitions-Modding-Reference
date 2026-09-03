using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters.Appearance;
using Controllers;
using Helpers;
using UI;
using UI.MiniMenu;
using UI.Notification;
using UnityEngine;

public class BusinessEmployeeController : ItemController
{
	protected string employeeName;

	protected string employeeSkill;

	[SerializeField]
	private Transform customerChairAttachmentPoint;

	[SerializeField]
	private Transform employeeChairAttachmentPoint;

	[SerializeField]
	private bool randomizeAppearance = true;

	protected ThirdPersonCharacter employeeTpc;

	private Transform _employeeChair;

	private Transform _customerChair;

	private Coroutine _pauseGameCoroutine;

	public string EmployeeName => employeeName;

	public override void Start()
	{
		base.Start();
		if (BuildingManager.CanBuildOnCurrentBuilding)
		{
			return;
		}
		if (employeeChairAttachmentPoint != null)
		{
			if (employeeChairAttachmentPoint.childCount < 2)
			{
				Debug.LogError("No chairs assigned to employee chair attachment");
				return;
			}
			_employeeChair = employeeChairAttachmentPoint.GetChild(1);
			_employeeChair.GetComponent<SeatController>().UpdateRestingAvailability();
		}
		if (customerChairAttachmentPoint != null)
		{
			if (customerChairAttachmentPoint.childCount < 2)
			{
				Debug.LogError("No chairs assigned to customer chair attachment");
				return;
			}
			_customerChair = customerChairAttachmentPoint.GetChild(1);
			_customerChair.GetComponent<SeatController>().UpdateRestingAvailability();
		}
		employeeTpc = PrefabHelper.CreatePrefab<ThirdPersonCharacter>("Characters/HumanDefinitionLow", base.transform);
		employeeTpc.gameObject.SetActive(value: true);
		string[] array = customValue.Split(",");
		if (array.Length > 1)
		{
			string text = array[1];
			if (!string.IsNullOrEmpty(text))
			{
				if (SpecialNpcHelper.SpecialNpcDataDictionary.TryGetValue(text, out var value))
				{
					employeeTpc.appearanceSetter.SetAppearance(value.characterData);
					employeeName = value.nameLocalizationKey;
				}
				else
				{
					Debug.LogError("Custom character data: " + text + " not found");
				}
			}
		}
		else if (randomizeAppearance)
		{
			employeeTpc.appearanceSetter.SetRandomAppearance();
			List<AppearanceElementData> uniformElements = EmployeeHelper.GetUniformElements(new List<string> { employeeSkill }, employeeTpc.appearanceSetter.data.gender);
			if (uniformElements != null)
			{
				employeeTpc.appearanceSetter.UpdateElements(uniformElements);
			}
		}
		employeeTpc.SitOnChair(_employeeChair);
		employeeTpc.SetItemIKTargets(GetEmployeeIKItemController());
	}

	private ItemController GetEmployeeIKItemController()
	{
		if (HasIKAttachmentPoint(this))
		{
			return this;
		}
		ItemController[] componentsInChildren = GetComponentsInChildren<ItemController>(includeInactive: false);
		foreach (ItemController itemController in componentsInChildren)
		{
			if (HasIKAttachmentPoint(itemController))
			{
				return itemController;
			}
		}
		return null;
	}

	private static bool HasIKAttachmentPoint(ItemController itemController)
	{
		if (!itemController.LHandIKAttachmentPoint && !itemController.RHandIKAttachmentPoint)
		{
			return itemController.HeadIKAttachmentPoint;
		}
		return true;
	}

	public override bool Interact()
	{
		if (InstanceBehavior<UIs>.Instance.playerHUD.dialogUI.isPanelOpen)
		{
			return true;
		}
		if (SaveGameManager.Current.ActiveVehicleId != null)
		{
			string vehicleTypeName = SaveGameManager.Current.VehicleInstances.Single((VehicleInstance x) => x.id == SaveGameManager.Current.ActiveVehicleId).vehicleTypeName;
			Dictionary<string, string> notificationData = new Dictionary<string, string> { { "vehicle", vehicleTypeName } };
			Notifications.Show(NotificationType.Error, "businessemployeecontroller_notification_cant_use_with_vehicle", notificationData);
			return true;
		}
		if (PlayerHelper.ItemInstanceInHands != null && !PlayerHelper.IsHoldingBag)
		{
			Notifications.ShowError("businessemployeecontroller_notification_cant_use_with_item_in_hand");
			return true;
		}
		if (base.Interact())
		{
			return true;
		}
		return false;
	}

	protected void SitPlayerOnChair()
	{
		Transform sittingPosition = _customerChair.GetComponent<SeatController>().SittingPosition;
		InstanceBehavior<GameManager>.Instance.playerController.Character.SitOnChair(sittingPosition);
	}

	public override Vector3 GetNavMeshTargetPosition(int index = 0)
	{
		if (!(_customerChair == null))
		{
			return _customerChair.GetComponent<EntityController>().GetNavMeshTargetPosition();
		}
		return customerChairAttachmentPoint.position;
	}

	protected void PauseGame()
	{
		if (InstanceBehavior<UIs>.Instance.playerHUD.dialogUI.isPanelOpen)
		{
			InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: true);
			_pauseGameCoroutine = StartCoroutine(PauseAfterDelayCoroutine(1f));
		}
	}

	protected void UnpauseGame()
	{
		if (!MiniMenu.IsOpen)
		{
			if (_pauseGameCoroutine != null)
			{
				StopCoroutine(_pauseGameCoroutine);
			}
			InstanceBehavior<UIs>.Instance.gameSpeed.SetPause(newPause: false);
			InstanceBehavior<UIs>.Instance.gameSpeed.DisableTimeControl(disabled: false);
		}
	}

	private IEnumerator PauseAfterDelayCoroutine(float delay)
	{
		yield return new WaitForSeconds(delay);
		InstanceBehavior<UIs>.Instance.gameSpeed.SetPause(newPause: true, showOverlay: false);
		_pauseGameCoroutine = null;
	}

	public SeatController GetCustomerChairController()
	{
		if (customerChairAttachmentPoint == null)
		{
			return null;
		}
		if (customerChairAttachmentPoint.childCount == 0)
		{
			return null;
		}
		return customerChairAttachmentPoint.GetChild(0).GetComponent<SeatController>();
	}
}
