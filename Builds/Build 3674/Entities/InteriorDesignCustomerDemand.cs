using System.Collections.Generic;
using BigAmbitions.Items;
using InteriorDesign;
using UnityEngine;

namespace Entities;

[CreateAssetMenu(fileName = "InteriorDesignCustomerDemand", menuName = "BigAmbitions/CustomerDemands/InteriorDesign")]
public class InteriorDesignCustomerDemand : CustomerDemand
{
	public override bool Fulfilled(BuildingRegistration registration, HashSet<Item> items)
	{
		return InteriorScoreCalculator.GetInteriorScorePercentage(registration.interiorDesigns) >= NeighborhoodHelper.GetData(registration.Neighborhood).minimumInteriorScore;
	}
}
