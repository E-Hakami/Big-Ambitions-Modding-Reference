// BigAmbitions.ModAPI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BAModAPI.IModLogger
using System;

public interface IModLogger
{
	void Info(string message);

	void Warn(string message);

	void Error(string message);

	void Error(Exception exception);
}
