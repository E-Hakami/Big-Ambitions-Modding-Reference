using System;
using BigAmbitions.Factories.Recipes;
using Controllers;
using Entities;
using Factories;
using Factories.Timeline;
using UnityEngine;
using UnityEngine.Playables;

namespace Items.SpecialItems;

public class FactoryAssemblyMachineController : EmployeeStationController
{
	private const int NumberOfAttachmentPoints = 2;

	[SerializeField]
	private PlayableDirector director;

	[SerializeField]
	private SpawnMorphPlayerData playerData;

	[SerializeField]
	private SpawnMorphPlayerData outputMachinePlayerData;

	[SerializeField]
	private AudioSource[] sfxAudioSources;

	private FactoryEmployee _factoryEmployee;

	public FactoryWorkstationInstance WorkstationInstance { get; private set; }

	public override bool Occupied
	{
		get
		{
			return occupied;
		}
		set
		{
			occupied = value;
		}
	}

	public override void Start()
	{
		base.Start();
		WorkstationInstance = base.ItemInstance as FactoryWorkstationInstance;
		GameEvent.onGameEventTriggered = (Action<string>)Delegate.Combine(GameEvent.onGameEventTriggered, new Action<string>(OnGameEventTriggered));
		employeeType = typeof(FactoryEmployee);
		TryStartVisuals();
		AudioSource[] array = sfxAudioSources;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].outputAudioMixerGroup = InstanceBehavior<GlobalReferences>.Instance.factoryMachinesMixerGroup;
		}
	}

	public override void OnDestroy()
	{
		base.OnDestroy();
		GameEvent.onGameEventTriggered = (Action<string>)Delegate.Remove(GameEvent.onGameEventTriggered, new Action<string>(OnGameEventTriggered));
	}

	private void OnGameEventTriggered(string gameEvent)
	{
		switch (gameEvent)
		{
		case "ba:gameevent_itemcargochanged":
		case "ba:gameevent_newhour":
		case "ba:gameevent_onfactorymachinerecipechanged":
		case "ba:gameevent_changedbusinessopenstate":
		case "ba:gameevent_itemdropped":
		case "ba:gameevent_timemachineended":
			TryStartVisuals();
			break;
		}
	}

	public override void UnassignEmployee()
	{
		base.UnassignEmployee();
		_factoryEmployee = null;
		TryStartVisuals();
	}

	public override void AssignEmployee(ThirdPersonCharacter tpc, EmployeeInstance newEmployeeInstance)
	{
		base.AssignEmployee(tpc, newEmployeeInstance);
		employee.SetEmployeeStation(this);
		_factoryEmployee = employee as FactoryEmployee;
		TryStartVisuals();
	}

	public void TryStartVisuals()
	{
		Recipe selectedRecipe = WorkstationInstance.SelectedRecipe;
		BuildingRegistration registration = base.BuildingContext.Registration;
		if (!WorkstationInstance.IsWorkstationActive(registration))
		{
			_factoryEmployee?.StopWorking();
			StopVisuals();
			return;
		}
		_factoryEmployee?.ResumeWorking();
		int num = Mathf.Max(Mathf.FloorToInt((float)director.duration * 1000f), 1);
		int num2 = UnityEngine.Random.Range(0, num);
		FactoryProductionMachineController[] attachedMachines = GetAttachedMachines();
		for (int i = 0; i < attachedMachines.Length; i++)
		{
			FactoryProductionMachineController obj = attachedMachines[i];
			int num3 = i * 1000 + num2;
			if (num3 >= num)
			{
				num3 %= num;
			}
			num2 = num3;
			obj?.TryStartVisuals(selectedRecipe, num2);
		}
		string startItemA = ((attachedMachines[0] != null) ? selectedRecipe.GetMachineVisual(attachedMachines[0].itemName).outputItemName : null);
		string startItemB = ((attachedMachines[1] != null) ? selectedRecipe.GetMachineVisual(attachedMachines[1].itemName).outputItemName : null);
		string outputItemName = selectedRecipe.GetMachineVisual(itemName).outputItemName;
		StartTimelineWithItems(startItemA, startItemB, outputItemName, num2);
	}

	public FactoryProductionMachineController[] GetAttachedMachines()
	{
		FactoryProductionMachineController[] array = new FactoryProductionMachineController[2];
		for (int i = 0; i < 2; i++)
		{
			if (WorkstationInstance.stackedItems.Count <= i)
			{
				continue;
			}
			AttachableChild attachableChild = WorkstationInstance.stackedItems[i];
			if (attachableChild != null)
			{
				ItemController itemControllerByID = ItemHelper.GetItemControllerByID(attachableChild.childId);
				if (!(itemControllerByID == null) && itemControllerByID is FactoryProductionMachineController factoryProductionMachineController)
				{
					array[attachableChild.attachmentIndex] = factoryProductionMachineController;
				}
			}
		}
		return array;
	}

	private void StartTimelineWithItems(string startItemA, string startItemB, string endItem, int startTimeMs)
	{
		playerData.SetSpawnMorphTrackItems(startItemA, startItemB, endItem);
		outputMachinePlayerData.SetSpawnMorphTrackItems(endItem);
		director.Restart(startTimeMs);
	}

	private void StopVisuals()
	{
		if ((bool)director && director.state == PlayState.Playing)
		{
			director.Stop();
			director.time = 0.0;
		}
		FactoryProductionMachineController[] attachedMachines = GetAttachedMachines();
		for (int i = 0; i < attachedMachines.Length; i++)
		{
			attachedMachines[i]?.StopVisuals();
		}
	}
}
