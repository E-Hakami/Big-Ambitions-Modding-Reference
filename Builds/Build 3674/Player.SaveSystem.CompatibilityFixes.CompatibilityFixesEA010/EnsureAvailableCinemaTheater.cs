using System.Collections.Generic;
using System.Linq;
using AI.Citizens;
using Helpers;
using UnityEngine;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA010;

public class EnsureAvailableCinemaTheater : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		CitizenHelper.Init();
		ApplyFix(gameInstance, "ba:buildingtype_cinema");
		ApplyFix(gameInstance, "ba:buildingtype_theater");
	}

	public static void ApplyFix()
	{
		ApplyFix(SaveGameManager.Current, "ba:buildingtype_cinema");
		ApplyFix(SaveGameManager.Current, "ba:buildingtype_theater");
	}

	private static void ApplyFix(GameInstance gameInstance, string buildingType)
	{
		List<BuildingRegistration> list = gameInstance.BuildingRegistrations.Where((BuildingRegistration x) => x.BuildingCached.BuildingType == buildingType).ToList();
		if (list.Count == 0 || list.Any((BuildingRegistration x) => x.RentedByPlayer || IsAvailableAndOnSaleByCity(x)) || list.Exists((BuildingRegistration x) => x.BuildingOwnedByPlayer))
		{
			return;
		}
		BuildingRegistration buildingRegistration = list.FirstOrDefault((BuildingRegistration x) => x.AvailableForRent && !x.BuildingOwnedByPlayer);
		if (buildingRegistration != null)
		{
			if (!buildingRegistration.IsOnSale())
			{
				RealEstateHelper.SetBuildingForSale(buildingRegistration);
			}
			buildingRegistration.buildingOwnerRivalId = string.Empty;
			Debug.Log($"Set building {buildingRegistration.Address} of type {buildingType} to be on sale by city.");
			return;
		}
		buildingRegistration = list.First();
		buildingRegistration.ShutDownAIBusiness();
		if (!buildingRegistration.IsOnSale())
		{
			RealEstateHelper.SetBuildingForSale(buildingRegistration);
		}
		buildingRegistration.buildingOwnerRivalId = string.Empty;
		Debug.Log($"Set {buildingType} {buildingRegistration.Address} to be available for rent.");
		static bool IsAvailableAndOnSaleByCity(BuildingRegistration x)
		{
			if (x.AvailableForRent && string.IsNullOrEmpty(x.buildingOwnerRivalId))
			{
				return x.IsOnSale();
			}
			return false;
		}
	}
}
