using System.Collections.Generic;
using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA06;

public class InitializeMovingServiceContracts : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		gameInstance.movingServiceContracts = new List<MovingServiceContract>();
	}
}
