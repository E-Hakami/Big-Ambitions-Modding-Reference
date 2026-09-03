using System;
using Enums;

namespace Entities;

[Serializable]
public class TodoTask
{
	public string id;

	public TodoTaskType type;

	public Address address;

	public string itemName;

	public string itemInstanceId;

	public string employeeId;

	public Priority priority;

	public int priorityOffset;

	public int remainingDays;

	public string businessRequirement;

	public static TodoTask GetTaskOfType(TodoTaskType taskType)
	{
		for (int i = 0; i < SaveGameManager.Current.TodoTasks.Count; i++)
		{
			TodoTask todoTask = SaveGameManager.Current.TodoTasks[i];
			if (todoTask.type == taskType)
			{
				return todoTask;
			}
		}
		return null;
	}
}
