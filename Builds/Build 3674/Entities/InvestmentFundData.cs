using System.Collections.Generic;
using UnityEngine;

namespace Entities;

[CreateAssetMenu(fileName = "New Investment Fund", menuName = "BigAmbitions/Investment Fund")]
public class InvestmentFundData : ScriptableObject
{
	public string fundName;

	public List<int> yearlyMarketChanges;

	public Address bankAddress;
}
