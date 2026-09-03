// BigAmbitions.ModAPI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BAModAPI.UnityModLogger
using System;
using BAModAPI;
using UnityEngine;

public sealed class UnityModLogger : IModLogger
{
	private readonly string _prefix;

	public UnityModLogger(string prefix)
	{
		_prefix = prefix;
	}

	public void Info(string message)
	{
		Debug.Log(_prefix + message);
	}

	public void Warn(string message)
	{
		Debug.LogWarning(_prefix + message);
	}

	public void Error(string message)
	{
		Debug.LogError(_prefix + message);
	}

	public void Error(Exception exception)
	{
		Debug.LogException(exception);
	}
}
