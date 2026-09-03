using System;
using BigAmbitions.SaveSystem;
using Buildings;
using Extensions;

namespace Blueprints;

[Serializable]
public class BuildingSizeInfo : IEquatable<BuildingSizeInfo>
{
	public string buildingSize;

	public int buildingVersion;

	public BuildingSizeInfo(string buildingSize, int buildingVersion)
	{
		this.buildingSize = buildingSize;
		this.buildingVersion = buildingVersion;
	}

	public BuildingSizeInfo(Building building)
	{
		buildingSize = building.BuildingSize;
		buildingVersion = building.BuildingVersion;
	}

	public BuildingSizeInfo(BuildingRegistration registration)
	{
		buildingSize = registration.BuildingCached.BuildingSize;
		buildingVersion = registration.BuildingCached.BuildingVersion;
	}

	public BuildingSizeInfo(BusinessLayoutSet layoutSet)
	{
		buildingSize = layoutSet.BuildingSize;
		buildingVersion = layoutSet.BuildingVersion;
	}

	public new string ToString()
	{
		return $"{GetSizeShort()}{buildingVersion}";
	}

	public bool Equals(BuildingSizeInfo other)
	{
		if (other != null && buildingSize == other.buildingSize)
		{
			return buildingVersion == other.buildingVersion;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is BuildingSizeInfo other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return HashCode.Combine(buildingSize, buildingVersion);
	}

	public string GetSizeShort()
	{
		return buildingSize.GetIdWithoutType().CapitalizeFirstChar();
	}
}
