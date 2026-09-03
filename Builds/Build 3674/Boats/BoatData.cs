using System;
using Entities;

namespace Boats;

[Serializable]
public class BoatData
{
	public BoatTypeName type;

	public string id;

	public string boatColorName;

	public int nextMaintenanceDay;
}
