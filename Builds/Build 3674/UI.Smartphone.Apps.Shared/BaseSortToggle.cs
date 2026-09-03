using System;
using System.Collections.Generic;
using UI.Components;

namespace UI.Smartphone.Apps.Shared;

public abstract class BaseSortToggle<TModel> : StateButtonBehaviour
{
	protected const int StateOff = 0;

	protected const int StateAscending = 1;

	protected const int StateDescending = 2;

	public bool IsOn => state != 0;

	public abstract void SetUp(int index, Action<int> onStateChanged);

	public abstract void Sort(ref List<TModel> items, string context);
}
