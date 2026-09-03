using Buildings;
using Helpers;

public class BuildingContext
{
	public Building Building { get; }

	public BuildingRegistration Registration { get; }

	public BusinessType BusinessType { get; private set; }

	public MultipleHeightsBuildingController MultipleHeights { get; }

	public bool IsPlayerOwnedBusiness => Registration?.RentedByPlayer ?? false;

	public BuildingContext(Building building, BuildingRegistration registration, BusinessType businessType, MultipleHeightsBuildingController multipleHeights)
	{
		Building = building;
		Registration = registration;
		BusinessType = businessType;
		MultipleHeights = multipleHeights;
	}

	public void RegenerateFieldsOnRegistrationChange()
	{
		BusinessType = BusinessTypeHelper.GetData(Registration);
	}
}
