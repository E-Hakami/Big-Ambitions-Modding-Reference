using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Text;
using UnityEngine;

public static class DebugLogCollector
{
	private class DebugMessage
	{
		public readonly string logString;

		public readonly string stackTrace;

		public readonly LogType type;

		public int amount = 1;

		public DebugMessage(string logString, string stackTrace, LogType type)
		{
			this.logString = logString;
			this.stackTrace = stackTrace;
			this.type = type;
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(logString, stackTrace, (int)type);
		}

		public override string ToString()
		{
			string text = type.ToStringFast().ToUpper() + ": " + logString + " " + stackTrace;
			if (amount != 1)
			{
				text = $"[{amount}]" + text;
			}
			return text;
		}
	}

	private static readonly ConcurrentQueue<DebugMessage> DebugMessages = new ConcurrentQueue<DebugMessage>();

	private static LogType[] LogTypes = new LogType[5]
	{
		LogType.Error,
		LogType.Assert,
		LogType.Warning,
		LogType.Log,
		LogType.Exception
	};

	private static readonly string[] LogMessagesToIgnore = new string[1] { "A Line Renderer component should be on a RectTransform positioned at (0,0,0), do not use in child Objects.\nFor best results, create separate RectTransforms as children of the canvas positioned at (0,0) for a UILineRenderer and do not move." };

	[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
	private static void Init()
	{
		Application.logMessageReceivedThreaded -= HandleLog;
		Application.logMessageReceivedThreaded += HandleLog;
		DebugMessages.Clear();
	}

	private static void HandleLog(string logString, string stackTrace, LogType type)
	{
		if (!LogTypes.Contains(type) || LogMessagesToIgnore.Contains(logString))
		{
			return;
		}
		DebugMessage debugMessage = new DebugMessage(logString, stackTrace, type);
		int hashCode = debugMessage.GetHashCode();
		foreach (DebugMessage item in DebugMessages.TakeLast(10))
		{
			if (item.GetHashCode() == hashCode)
			{
				lock (DebugMessages)
				{
					item.amount++;
					return;
				}
			}
		}
		DebugMessages.Enqueue(debugMessage);
	}

	public static string GetAllLogs()
	{
		lock (DebugMessages)
		{
			int count = DebugMessages.Count;
			int num = 0;
			int length = Environment.NewLine.Length;
			for (int i = 0; i < count; i++)
			{
				DebugMessage debugMessage = DebugMessages.ElementAt(i);
				num += debugMessage.logString.Length + debugMessage.stackTrace.Length + length * 3 + 10 + 4;
			}
			num += length * 100;
			StringBuilder stringBuilder = new StringBuilder(num);
			foreach (DebugMessage debugMessage2 in DebugMessages)
			{
				if (debugMessage2.amount > 1)
				{
					stringBuilder.Append("[").Append(debugMessage2.amount).Append("x] ");
				}
				stringBuilder.Append(debugMessage2.type.ToStringFast().ToUpper()).Append(": ").AppendLine(debugMessage2.logString)
					.AppendLine(debugMessage2.stackTrace);
			}
			return stringBuilder.ToString();
		}
	}

	public static void SetLogTypes(params LogType[] logTypes)
	{
		LogTypes = logTypes;
	}
}
