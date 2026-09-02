// BigAmbitions.ModAPI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BAModAPI.ModExtensionMethods
using System;
using UnityEngine;

public static class ModExtensionMethods
{
	public static void InvokeSafely(this Action callbacks)
	{
		if (callbacks == null)
		{
			return;
		}
		Delegate[] invocationList = callbacks.GetInvocationList();
		for (int i = 0; i < invocationList.Length; i++)
		{
			try
			{
				((Action)invocationList[i])();
			}
			catch (Exception exception)
			{
				Debug.LogException(exception);
			}
		}
	}
}
