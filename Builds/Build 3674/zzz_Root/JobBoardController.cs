using Controllers;
using UI;

public class JobBoardController : ItemWithTextController
{
	private Job _job;

	protected override void OnEnable()
	{
		base.OnEnable();
		_job = JobHelper.GetJob(InstanceBehavior<BuildingManager>.Instance.buildingRegistration.Address);
	}

	public void ShowJob()
	{
		if (HasJob())
		{
			InstanceBehavior<UIs>.Instance.playerHUD.ShowJob(_job);
		}
	}

	public bool HasJob()
	{
		return _job != null;
	}

	public void ShowCandidates()
	{
		InstanceBehavior<UIs>.Instance.fullMenu.myEmployees.initialTab = "Candidates";
		InstanceBehavior<UIs>.Instance.fullMenu.ShowApp(AppName.MyEmployees);
	}
}
