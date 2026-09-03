public struct EmployeeInstancesQueryInfo
{
	public Address withAssignedAddress;

	public bool excludeAbsentsNotReplaced;

	public bool excludeBeingReplaced;

	public bool isAssignedToAnyWorkShift;

	public bool isAssignedToAnyBusiness;

	public string[] withSkills;

	public string[] withoutSkills;
}
