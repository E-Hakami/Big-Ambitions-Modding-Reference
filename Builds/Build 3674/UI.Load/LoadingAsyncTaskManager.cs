using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UI.Load;

public static class LoadingAsyncTaskManager
{
	private static readonly List<Task> ActiveTasks = new List<Task>();

	public static void AddTask(Task task)
	{
		ActiveTasks.Add(task);
	}

	public static bool AreAllTasksCompleted()
	{
		return ActiveTasks.All((Task x) => x.IsCompleted);
	}

	public static void ClearTasks()
	{
		ActiveTasks.Clear();
	}
}
