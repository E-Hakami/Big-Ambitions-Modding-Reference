using Entities;

namespace Player.SaveSystem.CompatibilityFixes.CompatibilityFixesEA07;

public class UpdateTodoTasks : ICompatibilityFix
{
	public void Apply(GameInstance gameInstance)
	{
		foreach (TodoTask todoTask in gameInstance.TodoTasks)
		{
			switch ((int)todoTask.type)
			{
			case 13:
			case 15:
			case 17:
			case 18:
			case 19:
				todoTask.type = TodoTaskType.MissingRequiredItem;
				break;
			case 14:
				todoTask.type = TodoTaskType.EmployeeUnassigned;
				break;
			case 16:
				todoTask.type = TodoTaskType.BusinessTemporarilyClosed;
				break;
			}
		}
	}
}
