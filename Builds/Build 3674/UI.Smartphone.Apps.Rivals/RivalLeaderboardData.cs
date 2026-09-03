using System.Collections.Generic;
using UnityEngine;

namespace UI.Smartphone.Apps.Rivals;

public class RivalLeaderboardData
{
	public string rivalId;

	public string entryName;

	public Sprite portrait;

	public int ageInYears;

	public float weeklyIncome;

	public bool isDefeated;

	public List<BuildingRegistration> ownedBusinesses;

	public List<BuildingRegistration> ownedBuildings;

	public string mostActiveNeighborhood;
}
