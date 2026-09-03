// BigAmbitions.ModAPI, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null
// BAModAPI.IModBigAmbitions
using System.Threading.Tasks;
using BAModAPI;

public interface IModBigAmbitions
{
	string[] RelativeAssetBundlePaths { get; }

	Task OnLoadAsync(ModContext context);

	Task OnUnloadAsync();
}
