using System;
using BigAmbitions.Characters;
using Dialogs;
using Entities;
using JimmysUnityUtilities;
using UI;
using UI.Load;
using UnityEngine;

public class SpecialEmployeeController : BusinessEmployeeController
{
	public enum SpecialEmployeeType
	{
		Wholesale,
		Furniture,
		Import,
		Bank,
		MarketingAgency,
		RecruitmentAgency,
		InstallationFirm,
		MovingCompany,
		LongevityDoctor,
		HealthInsurance,
		VehicleStore,
		PrivateDriverService
	}

	private const float MinTimeBetweenAnims = 5f;

	private const float MaxTimeBetweenAnims = 15f;

	private SpecialEmployeeType _specialEmployeeType;

	private float _nextAnim;

	private float _animLength;

	public SpecialEmployeeType GetEmployeeType => _specialEmployeeType;

	public override void Start()
	{
		if (BuildingManager.CanBuildOnCurrentBuilding)
		{
			base.Start();
			return;
		}
		string[] array = customValue.Split(",");
		if (array.Length != 0)
		{
			string text = array[0];
			if (!string.IsNullOrEmpty(text))
			{
				if (Enum.TryParse(typeof(SpecialEmployeeType), text, out var result))
				{
					_specialEmployeeType = (SpecialEmployeeType)result;
					employeeName = GetEmployeeName();
					employeeSkill = GetEmployeeSkill();
				}
				else
				{
					Debug.LogWarning("Store type '" + text + "' not found (" + base.gameObject.name + ")");
				}
			}
		}
		base.Start();
		_nextAnim = UnityEngine.Random.Range(5f, 15f);
		_animLength = employeeTpc.animator.RunAnimationLength(AnimationType.UsingDesktopComputer);
	}

	private void Update()
	{
		if (!BuildingManager.CanBuildOnCurrentBuilding && !LoadScene.isLoading && (!InstanceBehavior<UIs>.Instance.playerHUD.dialogUI.isPanelOpen || !(InstanceBehavior<UIs>.Instance.playerHUD.dialogUI.npcInDialog == employeeTpc)))
		{
			if (_nextAnim <= 0f)
			{
				_nextAnim = UnityEngine.Random.Range(5f, 15f) + _animLength;
				employeeTpc?.animator.SetTrigger(AnimationType.UsingDesktopComputer);
			}
			_nextAnim -= Time.deltaTime;
		}
	}

	public override bool Interact()
	{
		if (base.Interact())
		{
			return true;
		}
		SitPlayerOnChair();
		CoroutineUtility.RunAfterSecondsDelay(base.PauseGame, 0.2f);
		InstanceBehavior<UIs>.Instance.playerHUD.dialogUI.ShowDialog(GetDialogType(), NavigationBlocker.SpecialEmployeeDialog, GetContact(), base.UnpauseGame, employeeTpc);
		return true;
	}

	private string GetEmployeeName()
	{
		return _specialEmployeeType switch
		{
			SpecialEmployeeType.Wholesale => "dialog_wholesale_store_npc_name", 
			SpecialEmployeeType.Furniture => "dialog_furniture_store_npc_name", 
			SpecialEmployeeType.Import => "dialog_import_npc_name", 
			SpecialEmployeeType.Bank => "dialog_bank_npc_name", 
			SpecialEmployeeType.RecruitmentAgency => "dialog_recruitment_agency_npc_name", 
			SpecialEmployeeType.MarketingAgency => "dialog_marketing_agency_npc_name", 
			SpecialEmployeeType.InstallationFirm => "dialog_interior_installation_firm_npc_name", 
			SpecialEmployeeType.MovingCompany => "dialog_moving_company_npc_name", 
			SpecialEmployeeType.LongevityDoctor => "dialog_doctor_npc_name", 
			SpecialEmployeeType.HealthInsurance => "dialog_health_insurance_manager_npc_name", 
			SpecialEmployeeType.VehicleStore => "dialog_vehicle_store_npc_name", 
			SpecialEmployeeType.PrivateDriverService => "dialog_private_driver_service_npc_name", 
			_ => "", 
		};
	}

	private string GetEmployeeSkill()
	{
		if (_specialEmployeeType == SpecialEmployeeType.HealthInsurance)
		{
			return "ba:skill_programmer";
		}
		return "ba:skill_hrmanager";
	}

	private CallDialogType GetDialogType()
	{
		if (_specialEmployeeType != SpecialEmployeeType.LongevityDoctor)
		{
			return InstanceBehavior<BuildingManager>.Instance.buildingRegistration.GetCallDialogType();
		}
		return CallDialogType.DoctorDialog;
	}

	private Contact GetContact()
	{
		if (_specialEmployeeType != SpecialEmployeeType.LongevityDoctor)
		{
			return InstanceBehavior<BuildingManager>.Instance.buildingRegistration.GetOrAddBusinessContact(hasWelcomeMessages: true);
		}
		return null;
	}
}
