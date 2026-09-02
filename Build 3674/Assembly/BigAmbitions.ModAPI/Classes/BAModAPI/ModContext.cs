// BigAmbitions.ModAPI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BAModAPI.ModContext
using BAModAPI;

public sealed class ModContext
{
	public string ModRootPath { get; }

	public string ModId { get; }

	public IModLogger Logger { get; }

	public ModContext(string modRootPath, string modId, IModLogger logger)
	{
		ModRootPath = modRootPath;
		ModId = modId;
		Logger = logger;
	}
}
