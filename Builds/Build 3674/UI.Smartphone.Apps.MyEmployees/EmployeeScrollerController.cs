using System.Collections.Generic;
using System.Linq;
using Entities;
using Helpers;
using UI.Smartphone.Apps.Shared;
using UnityEngine;

namespace UI.Smartphone.Apps.MyEmployees;

public sealed class EmployeeScrollerController : BaseFilteredScrollerController<EmployeeCellView, EmployeeModel>
{
	[SerializeField]
	private EmployeeFilterController filterController;

	protected override BaseFilterController<EmployeeModel> FilterController => filterController;

	protected override void PopulateAllModels(List<EmployeeModel> allModels)
	{
		allModels.AddRange(from instance in EmployeeHelper.GetEmployeeInstances(new EmployeeInstancesQueryInfo
			{
				excludeBeingReplaced = true
			})
			where !instance.IsCandidate
			select new EmployeeModel(instance));
	}

	protected override string GetDataId(EmployeeModel model)
	{
		return model.employeeId;
	}

	public void RemoveEmployee(EmployeeInstance employeeInstance)
	{
		RemoveModels((EmployeeModel model) => model.employeeInstance == employeeInstance);
	}

	public void RefreshEmployeeModel(EmployeeInstance employeeInstance)
	{
		ReplaceModel(new EmployeeModel(employeeInstance));
	}
}
