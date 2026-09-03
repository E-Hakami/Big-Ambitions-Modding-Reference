using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BigAmbitions.Characters.Appearance;
using BigAmbitions.DayNightCycle;
using BigAmbitions.Items;
using BigAmbitions.PlacementSystem;
using BigAmbitions.Tags;
using Buildings;
using EmployeeStations;
using Entities;
using Helpers;
using JimmysUnityUtilities;
using Player.HUD.ItemInfoOverlays;
using PlayerActivity;
using UI;
using UI.InteriorDesigner;
using UI.Purchase;
using UnityEngine;
using UnityEngine.AI;

public class EmployeeStationController : Producer, IWaitingLineHolder
{
	[Header("EmployeeStationController")]
	public EnergyConsumption workEnergyConsumption;

	[SerializeField]
	protected WaitingLine waitingLine;

	[SerializeField]
	protected Transform employeeSpot;

	[SerializeField]
	private Transform firstCustomerSpot;

	[NonSerialized]
	public Employee employee;

	[NonSerialized]
	public Customer playerCustomer;

	[NonSerialized]
	public EmployeeInstance employeeInstance;

	private bool _isUpdatingEmployee;

	protected Type employeeType;

	public WaitingLine GetWaitingLine()
	{
		return waitingLine;
	}

	public Transform GetFirstCustomerSpot()
	{
		return firstCustomerSpot;
	}

	public Transform GetEmployeeSpot()
	{
		return employeeSpot;
	}

	public bool CanWork()
	{
		if (!BuildingTypeHelper.GetData(InstanceBehavior<BuildingManager>.Instance.building).requiredBuildingSkills.Contains("ba:skill_customerservice") || !ItemsGetter.GetByName(itemName).suitableSkills.Contains("ba:skill_customerservice"))
		{
			return false;
		}
		if (playerItemPurchaserSettings.enabled || PlayerHelper.ItemInstanceInHands != null || InstanceBehavior<GameManager>.Instance.selectedVehicle != null)
		{
			return false;
		}
		JobInstance playerJob = SaveGameManager.Current.JobInstances.SingleOrDefault((JobInstance x) => x.address == base.BuildingContext.Registration.Address && x.hired);
		return ShouldOpenWorkUI(playerJob);
	}

	public bool Work()
	{
		if (!CanWork())
		{
			return false;
		}
		MoveTowardsEntity(delegate
		{
			PlayerActivityUI.Show(new WorkActivity(SaveGameManager.Current.JobInstances.SingleOrDefault((JobInstance x) => x.address == base.BuildingContext.Registration.Address && x.hired), this));
		});
		return true;
	}

	public override void Start()
	{
		base.Start();
		if (!playerItemPurchaserSettings.enabled)
		{
			InitWaitingLine();
			CoroutineUtility.RunAfterOneFrame(delegate
			{
				UpdateEmployee();
			});
			GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Combine(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationListener));
			GlobalEvents.onNewHour = (Action)Delegate.Combine(GlobalEvents.onNewHour, new Action(OnNewHour));
			InstanceBehavior<BuildingManager>.Instance?.onUniformChanged.AddListener(OnUniformChanged);
		}
	}

	public override void OnDestroy()
	{
		if (!GameManager.isCitySceneBeingUnloaded)
		{
			GlobalEvents.onBuildingRegistrationChange = (Action<Address>)Delegate.Remove(GlobalEvents.onBuildingRegistrationChange, new Action<Address>(OnBuildingRegistrationListener));
			GlobalEvents.onNewHour = (Action)Delegate.Remove(GlobalEvents.onNewHour, new Action(OnNewHour));
			InstanceBehavior<BuildingManager>.Instance?.onUniformChanged.RemoveListener(OnUniformChanged);
			base.OnDestroy();
		}
	}

	private void OnNewHour()
	{
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			UpdateEmployee(isNewHour: true);
		});
	}

	private void OnBuildingRegistrationListener(Address _)
	{
		UpdateEmployee();
	}

	public virtual Vector3 GetEmployeePosition()
	{
		Vector3 position = ((employeeSpot != null) ? employeeSpot : base.transform).position;
		Vector3 vector = new Vector3(position.x, GetEmployeeFloorHeight(position), position.z);
		if (NavMesh.SamplePosition(vector, out var hit, 1.5f, -1))
		{
			return hit.position;
		}
		return vector;
	}

	private float GetEmployeeFloorHeight(Vector3 spotPosition)
	{
		if (parentItemController != null)
		{
			return parentItemController.transform.position.y;
		}
		if (employeeSpot == null && (base.Item.snapToCeiling || base.Item.wallMounted))
		{
			MultipleHeightsBuildingController multipleHeights = base.BuildingContext.MultipleHeights;
			if (!(multipleHeights != null))
			{
				return 0f;
			}
			return multipleHeights.GetFloorHeightForPosition(spotPosition);
		}
		return spotPosition.y;
	}

	protected virtual void InitWaitingLine()
	{
	}

	public override bool Interact()
	{
		if (!base.Interact())
		{
			return Work();
		}
		return true;
	}

	public virtual void AssignEmployee(ThirdPersonCharacter tpc, EmployeeInstance newEmployeeInstance)
	{
		if ((bool)employee && employee.employeeTpc != tpc)
		{
			return;
		}
		employeeInstance = newEmployeeInstance;
		if (!employee)
		{
			if ((object)employeeType == null)
			{
				employeeType = typeof(Employee);
			}
			employee = tpc.gameObject.AddComponent(employeeType) as Employee;
			employee.employeeInstance = newEmployeeInstance;
			employee.employeeStationController = this;
			SetEmployeeAppearance(tpc);
		}
		Occupied = true;
		if (employeeType == typeof(SelfServiceEmployee))
		{
			tpc.ForceToPosition(GetEmployeePosition());
		}
		tpc.SetItemIKTargets(this, smooth: true);
		Vector3 worldPosition = ((firstCustomerSpot != null) ? firstCustomerSpot.position : base.transform.position);
		worldPosition.y = tpc.transform.position.y;
		tpc.transform.LookAt(worldPosition);
		if (waitingLine != null)
		{
			if (waitingLine.data.customers.Count > 0)
			{
				waitingLine.customersManagement.ResetFirstCustomerState();
			}
			else
			{
				StartCoroutine(RedirectQueuesToThisEmployeeStation());
			}
		}
		InstanceBehavior<OverlayManager>.Instance.UpdateDynamicComponents(this, DynamicOverlayUpdateType.EmployeeUpdate);
	}

	public void SetEmployeeAppearance(ThirdPersonCharacter tpc, AppearanceElementType[] excludeTypes = null)
	{
		List<AppearanceElementData> elements = employeeInstance.characterData.elements;
		if (elements == null || elements.Count <= 0)
		{
			tpc.appearanceSetter.data = employeeInstance.characterData;
			tpc.appearanceSetter.skipMeshMerging = true;
			AppearanceTag[] tags = ((!(this is CleaningStationController)) ? (base.BuildingContext.BusinessType?.employeeDefaultAppearanceTags ?? new AppearanceTag[1] { AppearanceTag.Casual }) : new AppearanceTag[1] { AppearanceTag.Casual });
			tpc.appearanceSetter.SetRandomAppearance(employeeInstance.characterData.gender, tags);
			tpc.appearanceSetter.skipMeshMerging = false;
			employeeInstance.characterData = tpc.appearanceSetter.data;
		}
		string presetIdBeingUsed = GetPresetIdBeingUsed(GetSkillBeingUsed());
		List<AppearanceElementData> list = employeeInstance.GetUniformElements(presetIdBeingUsed);
		if (list != null && excludeTypes != null)
		{
			list = list.Where((AppearanceElementData x) => !excludeTypes.Contains(x.type)).ToList();
		}
		tpc.appearanceSetter.UpdateVisuals(list);
	}

	private bool ShouldOpenWorkUI(JobInstance playerJob)
	{
		if (base.Item.assignable && !base.Item.HasTag(TagRef.Itemtag.iscleaningstation) && (InstanceBehavior<BuildingManager>.Instance.IsPlayerOwnedBusiness || playerJob != null) && !PlayerActivityUI.IsPanelOpen && !PurchaseUI.IsPanelOpen)
		{
			return !InteriorDesignerUI.IsOpen;
		}
		return false;
	}

	private IEnumerator RedirectQueuesToThisEmployeeStation()
	{
		yield return new WaitForEndOfFrame();
		if (waitingLine.data.controllerNames == null)
		{
			yield break;
		}
		int spotsAvailable = waitingLine.data.spots.Count;
		foreach (WaitingLine item in from x in WaitingLinesHelper.FindWaitingLines(waitingLine.data.controllerNames)
			where x.data.customers.Count > 1
			orderby x.data.customers.Count descending
			select x)
		{
			if (spotsAvailable == 0)
			{
				break;
			}
			if (item != waitingLine)
			{
				item.RedirectCustomersToGivenWaitingLine(waitingLine, ref spotsAvailable);
			}
		}
	}

	public virtual void UnassignEmployee()
	{
		employeeInstance = null;
		if (employee == null)
		{
			return;
		}
		employee.employeeTpc.SetItemIKTargets(null, smooth: true);
		if (employee.CompareTag("Player"))
		{
			employee.employeeTpc.appearanceSetter.SetAppearance(PlayerHelper.CharacterData);
			employee.employeeTpc.SetHandContent(null);
			UnityEngine.Object.Destroy(employee);
		}
		else
		{
			UnityEngine.Object.Destroy(employee.gameObject);
			if (ShouldUpdateStockState())
			{
				UpdateSelectedStockOverlay();
			}
		}
		employee = null;
		Occupied = false;
		InstanceBehavior<OverlayManager>.Instance.UpdateDynamicComponents(this, DynamicOverlayUpdateType.EmployeeUpdate);
	}

	private IEnumerator MoveCustomersToAnotherWaitingLineIfNeeded()
	{
		if (!(waitingLine == null))
		{
			yield return new WaitForEndOfFrame();
			Customer[] array = waitingLine.data.customers.ToArray();
			foreach (Customer customer in array)
			{
				waitingLine.customersManagement.MoveCustomerToAnotherWaitingLine(customer);
			}
		}
	}

	public void UpdateEmployee(bool isNewHour = false)
	{
		if (!ShouldUpdateEmployee())
		{
			return;
		}
		if (!IsBuildingAvailableForThisStation() || ItemHelper.HasAnyMissingRequirements(base.ItemInstance))
		{
			if ((bool)employee && !employee.isPlayer)
			{
				HandleNoEmployeeScheduled();
			}
			return;
		}
		_isUpdatingEmployee = true;
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			_isUpdatingEmployee = false;
		});
		WorkShift employeeWorkShift = GetEmployeeWorkShift();
		this.employeeInstance = EmployeeHelper.GetEmployeeById(employeeWorkShift?.employeeId, showError: false);
		if (IsThereAnEmployeeScheduledAndAvailable(employeeWorkShift, this.employeeInstance))
		{
			if (ScheduledEmployeeChanged(employeeWorkShift))
			{
				if (employee.isPlayer)
				{
					if ((employeeWorkShift?.startingHour == TimeHelper.CurrentHour) & isNewHour)
					{
						InstanceBehavior<UIs>.Instance.playerActivityUI.CancelActivity();
					}
					return;
				}
				EmployeeInstance employeeInstance = this.employeeInstance;
				UnassignEmployee();
				this.employeeInstance = employeeInstance;
			}
			if (employee == null && this.employeeInstance.IsEmployeeAvailable())
			{
				ThirdPersonCharacter thirdPersonCharacter = PrefabHelper.CreatePrefab<ThirdPersonCharacter>("Characters/HumanDefinitionLow", base.transform);
				thirdPersonCharacter.gameObject.SetActive(value: true);
				thirdPersonCharacter.appearanceSetter.SetAppearance(this.employeeInstance.characterData);
				AssignEmployee(thirdPersonCharacter, this.employeeInstance);
			}
		}
		else
		{
			HandleNoEmployeeScheduled();
		}
	}

	private WorkShift GetEmployeeWorkShift()
	{
		int currentHour = TimeHelper.CurrentHour;
		return base.BuildingContext.Registration.scheduleDays.Find((ScheduleDay x) => x.day == (DayOfWeekOrdered)TimeHelper.GetDayOfWeekIndex(TimeHelper.GetDayOfWeek())).workShifts?.FirstOrDefault((WorkShift x) => x.itemInstanceId == base.ItemInstance.id && x.startingHour <= currentHour && x.endingHour > currentHour && !string.IsNullOrEmpty(x.employeeId));
	}

	private bool ScheduledEmployeeChanged(WorkShift employeeWorkShift)
	{
		if (employee != null)
		{
			return employeeWorkShift.employeeId != employee.employeeInstance.id;
		}
		return false;
	}

	private static bool IsThereAnEmployeeScheduledAndAvailable(WorkShift employeeWorkShift, EmployeeInstance employeeInstance)
	{
		if (employeeWorkShift != null && employeeWorkShift.employeeId != null && employeeInstance != null)
		{
			return employeeInstance.IsEmployeeAvailable();
		}
		return false;
	}

	private void HandleNoEmployeeScheduled()
	{
		if (employee != null)
		{
			if (employee.isPlayer)
			{
				if (InstanceBehavior<UIs>.Instance.playerActivityUI.GetCurrentActivityState != PlayerActivityState.Running)
				{
					StartCoroutine(MoveCustomersToAnotherWaitingLineIfNeeded());
				}
			}
			else
			{
				UnassignEmployee();
				StartCoroutine(MoveCustomersToAnotherWaitingLineIfNeeded());
			}
		}
		else
		{
			StartCoroutine(MoveCustomersToAnotherWaitingLineIfNeeded());
		}
	}

	protected virtual bool IsBuildingAvailableForThisStation()
	{
		if (InstanceBehavior<BuildingManager>.Instance.isOpen && !base.BuildingContext.Registration.temporarilyClosed)
		{
			return base.BuildingContext.Registration.AreWorkCriticalRequirementsMet();
		}
		return false;
	}

	private bool ShouldUpdateEmployee()
	{
		if (!base.gameObject.activeInHierarchy || !base.BuildingContext.IsPlayerOwnedBusiness || _isUpdatingEmployee || !base.Item.assignable || !base.BuildingContext.Registration.HasValidAddress)
		{
			return false;
		}
		return !IsItemBeingModifiedInPlacementMode();
	}

	private bool IsItemBeingModifiedInPlacementMode()
	{
		if (PlacementSystem.IsInPlacementMode)
		{
			if (PlacementSystem.CurrentPlaceableItemBeingPlaced.GetItemInstance() != base.ItemInstance)
			{
				if (parentItemController != null)
				{
					return PlacementSystem.CurrentPlaceableItemBeingPlaced.GetItemInstance() == parentItemController.ItemInstance;
				}
				return false;
			}
			return true;
		}
		return false;
	}

	public virtual void OnPlaceOrder(List<CargoInstance> orderedCargoInstances = null, List<VehicleInstance> orderedVehicleInstances = null)
	{
	}

	public virtual void OnOrderCancel()
	{
	}

	public virtual EmployeeInstance GetAIEmployeeInstance()
	{
		return EmployeeHelper.CreateAIEmployeeInstance("ba:skill_customerservice");
	}

	private void OnUniformChanged(string skillName, string presetId)
	{
		if (employee == null || employee.employeeInstance == null)
		{
			return;
		}
		string skillBeingUsed = GetSkillBeingUsed();
		if (string.IsNullOrEmpty(skillName) || !(skillBeingUsed != skillName))
		{
			string presetIdBeingUsed = GetPresetIdBeingUsed(skillBeingUsed);
			if (presetId == null || (presetIdBeingUsed != null && presetIdBeingUsed == presetId))
			{
				employee.employeeTpc.appearanceSetter.UpdateVisuals(employee.employeeInstance.GetUniformElements(presetId));
			}
		}
	}

	private string GetSkillBeingUsed()
	{
		string text = ItemsGetter.GetByName(itemName).suitableSkills.FirstOrDefault((string x) => employee.employeeInstance.HasSkill(x));
		if (string.IsNullOrEmpty(text))
		{
			return "ba:skill_customerservice";
		}
		return text;
	}

	private string GetPresetIdBeingUsed(string skillName)
	{
		return base.BuildingContext.Registration.uniformsBySkill.GetValueOrDefault(skillName);
	}
}
