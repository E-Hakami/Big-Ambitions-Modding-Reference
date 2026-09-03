// BigAmbitions.ModsInternal, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BigAmbitions.ModsInternal.DiscoveredModEntry
using System;
using BAModAPI;

public sealed class DiscoveredModEntry
{
	public string ModId { get; set; }

	public string ModFolder { get; set; }

	public string ModDisplayName { get; set; }

	public Type EntryType { get; set; }

	public ModActivationScope Scope { get; set; }
}
