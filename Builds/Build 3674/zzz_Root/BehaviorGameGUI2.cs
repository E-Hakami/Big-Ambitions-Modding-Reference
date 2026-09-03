using System.Collections.Generic;
using System.Linq;
using System.Text;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using IngameDebugConsole;
using UnityEngine;

public class BehaviorGameGUI2 : MonoBehaviour
{
	private BehaviorManager behaviorManager;

	[ConsoleMethod("ToggleBehavior", "Toggles Behavior Tree Overlays", new string[] { })]
	public void ToggleBehaviorOverlay()
	{
		base.enabled = !base.enabled;
	}

	public void Awake()
	{
		base.enabled = false;
	}

	public void Start()
	{
		behaviorManager = BehaviorManager.instance;
	}

	public void OnGUI()
	{
		if (behaviorManager == null)
		{
			behaviorManager = BehaviorManager.instance;
		}
		if (behaviorManager == null || GameManager.GetMainCamera() == null)
		{
			return;
		}
		StringBuilder stringBuilder = new StringBuilder();
		foreach (BehaviorManager.BehaviorTree behaviorTree in behaviorManager.BehaviorTrees)
		{
			foreach (Stack<int> item in behaviorTree.activeStack.Where((Stack<int> stack) => stack.Count != 0))
			{
				Task task = behaviorTree.taskList[item.Peek()];
				if (task.FriendlyName == "")
				{
					task.FriendlyName = task.GetType().Name;
				}
				stringBuilder.AppendLine(task.FriendlyName);
			}
			Vector2 vector = GUIUtility.ScreenToGUIPoint(GameManager.GetMainCamera().WorldToScreenPoint(behaviorTree.behavior.transform.position));
			GUIContent content = new GUIContent(stringBuilder.ToString());
			stringBuilder.Clear();
			Vector2 vector2 = GUI.skin.label.CalcSize(content);
			vector2.x += 14f;
			vector2.y += 5f;
			GUI.Box(new Rect(vector.x - vector2.x / 2f, (float)((double)Screen.height - (double)vector.y + (double)vector2.y / 2.0), vector2.x, vector2.y), content);
		}
	}
}
