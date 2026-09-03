using System.Linq;
using BaTable;
using EnhancedUI.EnhancedScroller;
using Helpers;

namespace UI.Smartphone.Apps.BizMan;

public class PrivateResidenceScrollerController : BaTable<PrivateResidenceCellView, PrivateResidenceCellView.PrivateResidenceModel>
{
	public void Load()
	{
		data.Clear();
		data = (from instance in SaveGameManager.Current.BuildingRegistrations
			where instance.RentedByPlayer && instance.BuildingCached.BuildingType == "ba:buildingtype_residential"
			select new PrivateResidenceCellView.PrivateResidenceModel(instance.Address, BuildingHelper.GetBuildingSquareMeters(instance.Address))).ToList();
		scroller.ReloadData();
	}

	public override float GetCellViewSize(EnhancedScroller scroller, int dataIndex)
	{
		return 100f;
	}

	protected override string GetDataId(PrivateResidenceCellView.PrivateResidenceModel model)
	{
		return model.Id;
	}
}
