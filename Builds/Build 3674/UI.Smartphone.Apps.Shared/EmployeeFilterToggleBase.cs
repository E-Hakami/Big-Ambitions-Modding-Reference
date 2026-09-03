namespace UI.Smartphone.Apps.Shared;

public abstract class EmployeeFilterToggleBase<TModel> : BaseFilterToggle<TModel> where TModel : BaseEmployeeModel
{
	private string _demand;

	private string _skill;

	private string _status;

	public void ConfigureDemand(string demand)
	{
		_demand = demand;
		_skill = null;
		label.Key = demand;
	}

	public void ConfigureSkill(string skill)
	{
		_skill = skill;
		_demand = null;
		label.Key = skill;
	}

	public void ConfigureStatus(string status)
	{
		_status = status;
		_demand = null;
		_skill = null;
		label.Key = status;
	}

	public override bool PassesFilter(TModel employee)
	{
		return ShouldKeep(employee);
	}

	private bool ShouldKeep(TModel employee)
	{
		if (!string.IsNullOrEmpty(_demand))
		{
			return employee.demands.Contains(_demand);
		}
		if (!string.IsNullOrEmpty(_skill))
		{
			return employee.primarySkillName == _skill;
		}
		if (!string.IsNullOrEmpty(_status))
		{
			return employee.statuses.Contains(_status);
		}
		return true;
	}
}
