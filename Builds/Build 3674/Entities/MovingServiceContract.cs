using System;
using System.Collections.Generic;
using Buildings.BuildingTypes.Special.MovingCompany;
using Helpers;
using Streets;
using UI.Smartphone.Apps.Contacts;

namespace Entities;

[Serializable]
public class MovingServiceContract
{
	public BuildingRegistration movingCompanyRegistration;

	public Address originMovingAddress;

	public Address destinationMovingAddress;

	public int movingDay;

	public int movingHour;

	public bool transferBizManSettings;

	public bool IsMovingDay => movingDay <= SaveGameManager.Current.Day;

	public bool IsMovingHour => movingHour <= SaveGameManager.Current.Hour;

	public void DoMove()
	{
		Contact contact = Contact.GetContact(movingCompanyRegistration, ContactCategoryName.FurnitureAndEquipment);
		BuildingRegistration buildingRegistration = BuildingHelper.GetBuildingRegistration(originMovingAddress);
		BuildingRegistration buildingRegistration2 = BuildingHelper.GetBuildingRegistration(destinationMovingAddress);
		if (!buildingRegistration.RentedByPlayer || !buildingRegistration2.RentedByPlayer)
		{
			HandleNonRentedBuilding(buildingRegistration, contact);
		}
		else if (IsInsideBuilding(originMovingAddress) || IsInsideBuilding(destinationMovingAddress))
		{
			HandleIsInsideBuilding(buildingRegistration, buildingRegistration2, contact);
		}
		else
		{
			MovingServiceHelper.SetMovingServiceData(originMovingAddress, destinationMovingAddress, movingCompanyRegistration, transferBizManSettings);
			MovingServiceHelper.MoveBusiness();
			SaveGameManager.Current.movingServiceContracts.Remove(this);
		}
		BuildingManager.RefreshHamptonsHouseBlockerCollider(originMovingAddress);
		BuildingManager.RefreshHamptonsHouseBlockerCollider(destinationMovingAddress);
	}

	private void HandleIsInsideBuilding(BuildingRegistration originBuildingRegistration, BuildingRegistration destinationBuildingRegistration, Contact movingCompanyContact)
	{
		string value = (IsInsideBuilding(originMovingAddress) ? originBuildingRegistration.GetComposedName() : destinationBuildingRegistration.GetComposedName());
		movingCompanyContact.SendMessage(new TextMessage("ba:messagetype_dialog_moving_service_cant_move_while_inside_building", new Dictionary<string, string> { { "businessName", value } }));
		movingDay = SaveGameManager.Current.Day + 1;
	}

	private void HandleNonRentedBuilding(BuildingRegistration originBuildingRegistration, Contact movingCompanyContact)
	{
		Address address = ((!originBuildingRegistration.RentedByPlayer) ? originMovingAddress : destinationMovingAddress);
		movingCompanyContact.SendMessage(new TextMessage("ba:messagetype_dialog_moving_service_cant_move_in_non_rented_building", new Dictionary<string, string> { 
		{
			"businessName",
			address.ToFormattedString()
		} }));
		SaveGameManager.Current.movingServiceContracts.Remove(this);
	}

	private bool IsInsideBuilding(Address address)
	{
		if (SaveGameManager.Current.CurrentStreetName == address.streetName)
		{
			return SaveGameManager.Current.CurrentStreetNumber == address.streetNumber;
		}
		return false;
	}
}
