using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using IngameDebugConsole;
using JimmysUnityUtilities;
using UnityEngine;

public static class FpsTesting
{
	private struct FpsTestResults(string waypoint, float averageFps, int frameDrops)
	{
		public readonly string waypoint = waypoint;

		public readonly float averageFps = averageFps;

		public readonly int frameDrops = frameDrops;
	}

	private static readonly WaitForSeconds TeleportWait = new WaitForSeconds(0.8f);

	[ConsoleMethod("StartFpsTest", "Starts the FPS test teleporting the player to every waypoint with 'FPSTest' in its name. At every waypoint, the game will run for the specified amount of seconds and then save the results to a CSV file.", new string[] { })]
	public static void StartFpsTest(int seconds)
	{
		CoroutineUtility.Run(FpsTestCoroutine(seconds));
	}

	private static IEnumerator FpsTestCoroutine(int seconds)
	{
		List<string> waypoints = GetWaypoints();
		List<FpsTestResults> results = new List<FpsTestResults>();
		Debug.Log("Starting FPS test...");
		foreach (string waypointName in waypoints)
		{
			GameManager.Command_TeleportPlayerToWaypoint(waypointName);
			yield return TeleportWait;
			int frames = 0;
			float time = 0f;
			float initialPeriod = 1f;
			int initialFrames = 0;
			float initialTime = 0f;
			while (initialTime < initialPeriod)
			{
				initialFrames++;
				initialTime += Time.unscaledDeltaTime;
				yield return null;
			}
			float num = (float)initialFrames / initialTime;
			float fpsThreshold = num * 0.8f;
			int frameDrops = 0;
			while (time < (float)seconds)
			{
				frames++;
				time += Time.unscaledDeltaTime;
				if (1f / Time.unscaledDeltaTime < fpsThreshold)
				{
					frameDrops++;
				}
				yield return null;
			}
			float averageFps = (float)frames / time;
			results.Add(new FpsTestResults(waypointName, averageFps, frameDrops));
		}
		StringBuilder stringBuilder = new StringBuilder();
		stringBuilder.AppendLine("Waypoint,Average FPS,Frame Drops");
		for (int i = 0; i < waypoints.Count; i++)
		{
			stringBuilder.AppendLine($"{results[i].waypoint},{results[i].averageFps},{results[i].frameDrops}");
		}
		File.WriteAllText("C://Downloads/fpsTestResults.csv", stringBuilder.ToString());
		Debug.Log("FPS test results saved to C://Downloads/fpsTestResults.csv");
	}

	private static List<string> GetWaypoints()
	{
		List<string> list = UnityEngine.PlayerPrefs.GetString("tpwWaypoints").Split(';').ToList();
		List<string> list2 = new List<string>();
		for (int i = 1; i < list.Count; i++)
		{
			string[] array = list[i].Split('|');
			if (array[0].ToLower().Contains("fpstest"))
			{
				list2.Add(array[0]);
			}
		}
		return list2;
	}
}
