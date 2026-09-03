using System;
using System.Collections.Generic;
using BigAmbitions.Characters.Skills;
using BigAmbitions.Factories;
using BigAmbitions.Factories.Recipes;
using BigAmbitions.Factories.Workstations;
using BigAmbitions.Items;
using BigAmbitions.Neighborhoods;
using BigAmbitions.Rivals;
using BigAmbitions.Tags;
using Buildings;
using Buildings.BuildingTypes.Office.Headquarters.Headhunter;
using Buildings.Office.Headquarters;
using Entities;
using Entities.Employee.JobDemands;
using Helpers;
using IngameDebugConsole;
using JimmysUnityUtilities;
using Streets;
using UI.Smartphone.Apps.MyEmployees;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Vehicles;
using Vehicles.VehicleTypes;

namespace BigAmbitions.SaveSystem;

public static class AddressableLoader
{
	private static readonly List<Func<(AsyncOperationHandle handle, Action complete)>> LoadingJobs = new List<Func<(AsyncOperationHandle, Action)>>();

	private static readonly List<AsyncOperationHandle> LoadedHandles = new List<AsyncOperationHandle>();

	public static void RegisterAndLoadAll()
	{
		LoadingJobs.Clear();
		RegisterAll();
		List<AsyncOperationHandle> handles = new List<AsyncOperationHandle>(LoadedHandles);
		ForceLoad();
		ReleaseHandles(handles);
		DebugLogConsole.Init();
		AddressableResolver.Init();
	}

	public static void RegisterMainMenu()
	{
		LoadingJobs.Clear();
		Register<BuildingSizeData>("BuildingSizes", BuildingSizeHelper.OnBuildingSizesLoaded);
		Register<BusinessType>("BusinessTypes", BusinessTypeHelper.OnBusinessTypesLoaded);
		Register<BuildingTypeData>("BuildingTypes", BuildingTypeHelper.OnBuildingTypesLoaded);
		Register<Item>("Items", ItemsGetter.OnItemsLoaded);
		List<AsyncOperationHandle> handles = new List<AsyncOperationHandle>(LoadedHandles);
		ForceLoad();
		ReleaseHandles(handles);
		AddressableResolver.Init();
	}

	public static void RegisterBlueprintCreator()
	{
		LoadingJobs.Clear();
		Register<TagDatabase>("TagDatabases", TagDatabaseHelper.OnTagDatabasesLoaded);
		Register<BuildingSizeData>("BuildingSizes", BuildingSizeHelper.OnBuildingSizesLoaded);
		Register<BuildingTypeData>("BuildingTypes", BuildingTypeHelper.OnBuildingTypesLoaded);
		Register<Item>("Items", ItemsGetter.OnItemsLoaded);
		Register<Recipe>("FactoryRecipes", FactoriesHelper.OnRecipesLoaded);
		Register<FactoryWorkstation>("FactoryWorkstations", FactoryWorkstationHelper.OnFactoryWorkstationsLoaded);
		Register<BusinessType>("BusinessTypes", BusinessTypeHelper.OnBusinessTypesLoaded);
		Register<VehicleType>("VehicleTypes", VehicleTypeHelper.OnVehicleTypesLoaded);
		List<AsyncOperationHandle> handles = new List<AsyncOperationHandle>(LoadedHandles);
		ForceLoad();
		ReleaseHandles(handles);
		AddressableResolver.Init();
	}

	public static void Register<T>(string label, Action<IList<T>> callback)
	{
		LoadingJobs.Add(delegate
		{
			AsyncOperationHandle<IList<T>> handle = Addressables.LoadAssetsAsync<T>(label, null);
			return ((AsyncOperationHandle handle, Action complete))(handle: handle, complete: delegate
			{
				IList<T> result = handle.Result;
				for (int i = 0; i < result.Count; i++)
				{
					if (result[i] is TagDatabase tagDatabase)
					{
						tagDatabase.Rebuild();
					}
				}
				for (int j = 0; j < result.Count; j++)
				{
					if (result[j] is TaggedScriptableObject taggedScriptableObject)
					{
						taggedScriptableObject.BuildTagCache();
					}
				}
				callback?.Invoke(result);
			});
		});
	}

	public static void ForceLoad()
	{
		int count = LoadingJobs.Count;
		if (count != 0)
		{
			(AsyncOperationHandle, Action)[] array = new(AsyncOperationHandle, Action)[count];
			for (int i = 0; i < count; i++)
			{
				array[i] = LoadingJobs[i]();
			}
			for (int j = 0; j < count; j++)
			{
				array[j].Item1.WaitForCompletion();
			}
			for (int k = 0; k < count; k++)
			{
				array[k].Item2();
			}
			CacheLoadedHandles(array);
			LoadingJobs.Clear();
			Debug.Log("All addressables loaded");
		}
	}

	private static void CacheLoadedHandles((AsyncOperationHandle handle, Action complete)[] startedJobs)
	{
		LoadedHandles.Clear();
		LoadedHandles.EnsureCapacity(startedJobs.Length);
		for (int i = 0; i < startedJobs.Length; i++)
		{
			LoadedHandles.Add(startedJobs[i].handle);
		}
	}

	private static void ReleaseHandles(List<AsyncOperationHandle> handles)
	{
		for (int i = 0; i < handles.Count; i++)
		{
			if (handles[i].IsValid())
			{
				Addressables.Release(handles[i]);
			}
		}
	}

	private static void ReleaseLoadedHandles()
	{
		ReleaseHandles(LoadedHandles);
		LoadedHandles.Clear();
	}

	private static void RegisterAll()
	{
		Register<TagDatabase>("TagDatabases", TagDatabaseHelper.OnTagDatabasesLoaded);
		Register<Building>("BuildingsDefinitions", BuildingHelper.OnBuildingsLoaded);
		Register<VehicleType>("VehicleTypes", VehicleTypeHelper.OnVehicleTypesLoaded);
		Register<Recipe>("FactoryRecipes", FactoriesHelper.OnRecipesLoaded);
		Register<FactoryWorkstation>("FactoryWorkstations", FactoryWorkstationHelper.OnFactoryWorkstationsLoaded);
		Register<BuildingSizeData>("BuildingSizes", BuildingSizeHelper.OnBuildingSizesLoaded);
		Register<SpecialRival>("Rivals", RivalsHelper.OnSpecialRivalsLoaded);
		Register<RivalsSettings>("RivalsSettings", RivalsHelper.OnRivalsSettingsLoaded);
		Register<BuildingTypeData>("BuildingTypes", BuildingTypeHelper.OnBuildingTypesLoaded);
		Register<EnergySettings>("EnergySettings", EnergyHelper.OnEnergySettingsLoaded);
		Register<HappinessModifier>("HappinessModifiers", HappinessHelper.OnHappinessModifiersLoaded);
		Register<CustomerDemand>(CustomerDemandHelper.AddressableLabel, CustomerDemandHelper.OnCustomerDemandsLoaded);
		Register<HeadhunterDealBreakerData>("HeadhunterDealBreakers", HeadhunterHelper.OnDealBreakersLoaded);
		Register<EmployeeMassAction>("EmployeeMassActions", EmployeeMassActionHelper.OnMassActionsLoaded);
		Register<SkillData>("Skills", SkillHelper.OnSkillDataLoaded);
		Register<TowDestinationData>("TowDestinations", TowDestinationHelper.OnTowDestinationsLoaded);
		Register<StreetData>("StreetData", AddressHelper.OnStreetDataLoaded);
		Register<NeighborhoodData>("Neighborhoods", NeighborhoodHelper.OnNeighborhoodsLoaded);
		Register<Item>("Items", ItemsGetter.OnItemsLoaded);
		Register<JobDemand>("JobDemands", JobDemandHelper.OnJobDemandsLoaded);
		Register<BusinessType>("BusinessTypes", BusinessTypeHelper.OnBusinessTypesLoaded);
		Register<InvestmentFundData>("InvestmentFunds", InvestmentFundHelper.OnInvestmentFundsLoaded);
		Register<DiplomaData>("Diploma", EducationHelper.OnDiplomasLoaded);
	}
}
