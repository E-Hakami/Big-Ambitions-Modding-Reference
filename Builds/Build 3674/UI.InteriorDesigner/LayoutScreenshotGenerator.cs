using System.IO;
using System.Threading.Tasks;
using BigAmbitions.InteriorDesigner;
using Blueprints;
using Buildings;
using Buildings.Indoors;
using Extensions;
using UnityEngine;

namespace UI.InteriorDesigner;

public class LayoutScreenshotGenerator : MonoBehaviour
{
	public const int ThumbnailWidth = 1920;

	public const int ThumbnailHeight = 1080;

	[SerializeField]
	private Camera layoutCamera;

	public async Task GenerateThumbnails(string thumbnailPath, BuildingSizeInfo sizeInfo)
	{
		Transform transform = InstanceBehavior<BuildingManager>.Instance.GetBuildingTransform(sizeInfo)?.Find("ThumbnailGeneratorData");
		if (transform == null)
		{
			Debug.LogError("Couldn't find ThumbnailGeneratorData for building " + sizeInfo.ToString());
			return;
		}
		Transform transform2 = layoutCamera.transform;
		transform2.position = transform.position;
		transform2.rotation = transform.rotation;
		base.gameObject.SetActive(value: true);
		WallsVisibility originalWallsVisibility = WallsVisibilityHelper.currentWallsVisibility;
		WallsVisibilityHelper.ToggleWalls(WallsVisibility.AllVisible);
		if (InstanceBehavior<BuildingManager>.Instance.multipleHeightsBuildingController != null)
		{
			InstanceBehavior<BuildingManager>.Instance.multipleHeightsBuildingController.OnCurrentHeightChanged(1);
			float buildingWallHeight = BuildingSizeHelper.GetBuildingWallHeight(sizeInfo.buildingSize, 0);
			transform2.SetPositionY(transform2.position.y + buildingWallHeight);
		}
		await Task.Delay(500);
		await GenerateThumbnail(thumbnailPath, 1920, 1080);
		WallsVisibilityHelper.ToggleWalls(originalWallsVisibility);
		base.gameObject.SetActive(value: false);
	}

	private async Task GenerateThumbnail(string savePath, int width, int height)
	{
		Rect rect = new Rect(0f, 0f, width, height);
		Texture2D texture = CaptureScreenRender(rect, width, height);
		Texture2D tex = new Texture2D(2, 2);
		byte[] array = texture.EncodeToJPG();
		tex.LoadImage(array);
		await File.WriteAllBytesAsync(savePath, array);
		Object.Destroy(texture);
	}

	private Texture2D CaptureScreenRender(Rect rect, int width, int height)
	{
		RenderTexture renderTexture = new RenderTexture(width, height, 24);
		Texture2D texture2D = new Texture2D(width, height, TextureFormat.ARGB32, mipChain: false);
		layoutCamera.targetTexture = renderTexture;
		layoutCamera.Render();
		RenderTexture.active = renderTexture;
		texture2D.ReadPixels(rect, 0, 0);
		layoutCamera.targetTexture = null;
		RenderTexture.active = null;
		Object.Destroy(renderTexture);
		return texture2D;
	}
}
