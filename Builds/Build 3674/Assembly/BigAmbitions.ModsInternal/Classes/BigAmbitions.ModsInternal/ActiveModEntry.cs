// BigAmbitions.ModsInternal, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.ModsInternal.ActiveModEntry
using BAModAPI;

public sealed class ActiveModEntry
{
	public string ModId { get; set; }

	public string ModFolder { get; set; }

	public string ModDisplayName { get; set; }

	public ModActivationScope Scope { get; set; }

	public IModBigAmbitions Instance { get; set; }
}
