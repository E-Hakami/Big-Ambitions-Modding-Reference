using System.Collections.Generic;
using System.Linq;
using BaTable;
using EnhancedUI.EnhancedScroller;
using JimmysUnityUtilities;

namespace UI.Smartphone.Apps.Rivals.Tables;

public class RivalBusinessesTable : BaTable<RivalBusinessesCellView, RivalBusinessesCellView.RivalBusinessModel>
{
	private void OnEnable()
	{
		CoroutineUtility.RunAfterOneFrame(delegate
		{
			Load(InstanceBehavior<RivalsApp>.Instance.selectedRival?.ownedBusinesses);
		});
	}

	public void Load(List<BuildingRegistration> registrations)
	{
		if (registrations != null)
		{
			data.Clear();
			foreach (BuildingRegistration registration in registrations)
			{
				data.Add(new RivalBusinessesCellView.RivalBusinessModel(registration.BusinessName, registration.businessTypeName, registration.RentedByPlayer ? registration.GetAvgWeeklyIncome() : registration.dailyIncomes.TakeLast(7).Sum(), registration.Address));
			}
		}
		ResetFilters();
		scroller.ReloadData();
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return 100f;
	}
}
