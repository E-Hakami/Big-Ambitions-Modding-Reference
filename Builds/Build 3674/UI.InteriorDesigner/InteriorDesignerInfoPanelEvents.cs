using System;
using UnityEngine;

namespace UI.InteriorDesigner;

public static class InteriorDesignerInfoPanelEvents
{
	public static Action<double> onPlayerBalanceChanged;

	public static Action<double> onCostBalanceChanged;

	public static Action<double> onBlueprintCostChanged;

	public static Action<float> onUpdateInteriorScore;

	public static Action onUpdateCustomerCapacity;

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void ResetStaticData()
	{
		onPlayerBalanceChanged = null;
		onCostBalanceChanged = null;
		onBlueprintCostChanged = null;
		onUpdateInteriorScore = null;
		onUpdateCustomerCapacity = null;
	}
}
