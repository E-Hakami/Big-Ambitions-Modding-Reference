using System.Collections.Generic;
using Entities;
using UnityEngine;

namespace Buildings;

[CreateAssetMenu(menuName = "BigAmbitions/SpecialService/MarketingAgencySettings")]
public class MarketingAgencySettings : SpecialServiceSettings
{
	public List<MarketingTypeName> marketingTypesAvailable;
}
