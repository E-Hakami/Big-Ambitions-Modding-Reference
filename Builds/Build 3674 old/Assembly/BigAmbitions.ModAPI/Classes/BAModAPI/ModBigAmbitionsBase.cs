// BigAmbitions.ModAPI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BAModAPI.ModBigAmbitionsBase
using System;
using System.Threading.Tasks;
using BAModAPI;

public abstract class ModBigAmbitionsBase : IModBigAmbitions
{
	public virtual string[] RelativeAssetBundlePaths => Array.Empty<string>();

	public abstract Task OnLoadAsync(ModContext context);

	public abstract Task OnUnloadAsync();
}
