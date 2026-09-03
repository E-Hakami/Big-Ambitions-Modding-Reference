using System.Collections.Generic;
using Entities;
using UI.Smartphone.Apps.Shared;
using UnityEngine;

namespace UI.Smartphone.Apps.MyEmployees;

public sealed class CandidateScrollerController : BaseFilteredScrollerController<CandidateCellView, CandidateModel>
{
	[SerializeField]
	private CandidateFilterController filterController;

	protected override BaseFilterController<CandidateModel> FilterController => filterController;

	protected override void PopulateAllModels(List<CandidateModel> allModels)
	{
		foreach (EmployeeInstance candidateEmployeeInstance in SaveGameManager.Current.CandidateEmployeeInstances)
		{
			if (candidateEmployeeInstance.IsCandidate)
			{
				allModels.Add(new CandidateModel(candidateEmployeeInstance));
			}
		}
	}

	protected override string GetDataId(CandidateModel model)
	{
		return model.employeeId;
	}

	public void RemoveCandidate(EmployeeInstance employeeInstance)
	{
		RemoveModels((CandidateModel model) => model.employeeInstance == employeeInstance);
	}
}
