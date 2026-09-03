using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

internal class SlightBlur : CustomPass
{
	private static class ShaderID
	{
		public static readonly int _BlitTexture = Shader.PropertyToID("_BlitTexture");

		public static readonly int _BlitScaleBias = Shader.PropertyToID("_BlitScaleBias");

		public static readonly int _BlitMipLevel = Shader.PropertyToID("_BlitMipLevel");

		public static readonly int _Radius = Shader.PropertyToID("_Radius");

		public static readonly int _Source = Shader.PropertyToID("_Source");

		public static readonly int _ColorBufferCopy = Shader.PropertyToID("_ColorBufferCopy");

		public static readonly int _Mask = Shader.PropertyToID("_Mask");

		public static readonly int _MaskDepth = Shader.PropertyToID("_MaskDepth");

		public static readonly int _InvertMask = Shader.PropertyToID("_InvertMask");

		public static readonly int _ViewPortSize = Shader.PropertyToID("_ViewPortSize");
	}

	[Range(0f, 16f)]
	public float radius = 4f;

	public bool useMask;

	public LayerMask maskLayer = 0;

	public bool invertMask;

	private Material compositeMaterial;

	private Material whiteRenderersMaterial;

	private RTHandle downSampleBuffer;

	private RTHandle blurBuffer;

	private RTHandle maskBuffer;

	private RTHandle maskDepthBuffer;

	private RTHandle colorCopy;

	private ShaderTagId[] shaderTags;

	[SerializeField]
	[HideInInspector]
	private Shader compositeShader;

	[SerializeField]
	[HideInInspector]
	private Shader whiteRenderersShader;

	protected override void Setup(ScriptableRenderContext renderContext, CommandBuffer cmd)
	{
		if (compositeShader == null)
		{
			compositeShader = Addressables.LoadAssetAsync<Shader>("CompositeBlur").WaitForCompletion();
		}
		if (whiteRenderersShader == null)
		{
			whiteRenderersShader = Shader.Find("Hidden/Renderers/WhiteRenderers");
		}
		compositeMaterial = CoreUtils.CreateEngineMaterial(compositeShader);
		whiteRenderersMaterial = CoreUtils.CreateEngineMaterial(whiteRenderersShader);
		downSampleBuffer = RTHandles.Alloc(Vector2.one * 0.5f, TextureXR.slices, DepthBits.None, GraphicsFormat.B10G11R11_UFloatPack32, FilterMode.Point, TextureWrapMode.Repeat, TextureXR.dimension, enableRandomWrite: false, useMipMap: false, autoGenerateMips: true, isShadowMap: false, 1, 0f, MSAASamples.None, bindTextureMS: false, useDynamicScale: true, RenderTextureMemoryless.None, VRTextureUsage.None, "DownSampleBuffer");
		blurBuffer = RTHandles.Alloc(Vector2.one * 0.5f, TextureXR.slices, DepthBits.None, GraphicsFormat.B10G11R11_UFloatPack32, FilterMode.Point, TextureWrapMode.Repeat, TextureXR.dimension, enableRandomWrite: false, useMipMap: false, autoGenerateMips: true, isShadowMap: false, 1, 0f, MSAASamples.None, bindTextureMS: false, useDynamicScale: true, RenderTextureMemoryless.None, VRTextureUsage.None, "BlurBuffer");
		shaderTags = new ShaderTagId[4]
		{
			new ShaderTagId("Forward"),
			new ShaderTagId("ForwardOnly"),
			new ShaderTagId("SRPDefaultUnlit"),
			new ShaderTagId("FirstPass")
		};
	}

	private void AllocateMaskBuffersIfNeeded()
	{
		if (useMask)
		{
			if (colorCopy?.rt == null || !colorCopy.rt.IsCreated())
			{
				RenderPipelineSettings.ColorBufferFormat colorBufferFormat = (GraphicsSettings.renderPipelineAsset as HDRenderPipelineAsset).currentPlatformRenderPipelineSettings.colorBufferFormat;
				Vector2 one = Vector2.one;
				int slices = TextureXR.slices;
				TextureDimension dimension = TextureXR.dimension;
				colorCopy = RTHandles.Alloc(one, slices, DepthBits.None, (GraphicsFormat)colorBufferFormat, FilterMode.Point, TextureWrapMode.Repeat, dimension, enableRandomWrite: false, useMipMap: false, autoGenerateMips: true, isShadowMap: false, 1, 0f, MSAASamples.None, bindTextureMS: false, useDynamicScale: true, RenderTextureMemoryless.None, VRTextureUsage.None, "Color Copy");
			}
			if (maskBuffer?.rt == null || !maskBuffer.rt.IsCreated())
			{
				maskBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, DepthBits.None, GraphicsFormat.R8_UNorm, FilterMode.Point, TextureWrapMode.Repeat, TextureXR.dimension, enableRandomWrite: false, useMipMap: false, autoGenerateMips: true, isShadowMap: false, 1, 0f, MSAASamples.None, bindTextureMS: false, useDynamicScale: true, RenderTextureMemoryless.None, VRTextureUsage.None, "Blur Mask");
			}
			if (maskDepthBuffer?.rt == null || !maskDepthBuffer.rt.IsCreated())
			{
				maskDepthBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, DepthBits.Depth16, GraphicsFormat.R16_UInt, FilterMode.Point, TextureWrapMode.Repeat, TextureXR.dimension, enableRandomWrite: false, useMipMap: false, autoGenerateMips: true, isShadowMap: false, 1, 0f, MSAASamples.None, bindTextureMS: false, useDynamicScale: true, RenderTextureMemoryless.None, VRTextureUsage.None, "Blur Depth Mask");
			}
		}
	}

	protected override void Execute(CustomPassContext ctx)
	{
		AllocateMaskBuffersIfNeeded();
		if (compositeMaterial != null && radius > 0f)
		{
			if (useMask)
			{
				CoreUtils.SetRenderTarget(ctx.cmd, maskBuffer, maskDepthBuffer, ClearFlag.All);
				CustomPassUtils.DrawRenderers(in ctx, maskLayer, RenderQueueType.All, null, 0, new RenderStateBlock(RenderStateMask.Depth)
				{
					depthState = new DepthState(writeEnabled: true, CompareFunction.LessEqual)
				});
			}
			GenerateGaussianMips(ctx);
		}
	}

	protected override void AggregateCullingParameters(ref ScriptableCullingParameters cullingParameters, HDCamera hdCamera)
	{
		cullingParameters.cullingMask |= (uint)maskLayer.value;
	}

	private void SetViewPortSize(CommandBuffer cmd, MaterialPropertyBlock block, RTHandle target)
	{
		Vector2Int scaledSize = target.GetScaledSize(target.rtHandleProperties.currentViewportSize);
		block.SetVector(ShaderID._ViewPortSize, new Vector4(scaledSize.x, scaledSize.y, 1f / (float)scaledSize.x, 1f / (float)scaledSize.y));
	}

	private void GenerateGaussianMips(CustomPassContext ctx)
	{
		RTHandle rTHandle = ((targetColorBuffer == TargetBuffer.Camera) ? ctx.cameraColorBuffer : ctx.customColorBuffer.Value);
		if (useMask)
		{
			for (int i = 0; i < rTHandle.rt.volumeDepth; i++)
			{
				ctx.cmd.CopyTexture(rTHandle, i, colorCopy, i);
			}
		}
		RTHandle destination = (useMask ? downSampleBuffer : rTHandle);
		CustomPassUtils.GaussianBlur(in ctx, rTHandle, destination, blurBuffer, 9, radius);
		if (useMask)
		{
			using (new ProfilingScope(ctx.cmd, new ProfilingSampler("Compose Mask Blur")))
			{
				MaterialPropertyBlock materialPropertyBlock = new MaterialPropertyBlock();
				materialPropertyBlock.SetFloat(ShaderID._Radius, radius / 4f);
				materialPropertyBlock.SetTexture(ShaderID._Source, downSampleBuffer);
				materialPropertyBlock.SetTexture(ShaderID._ColorBufferCopy, colorCopy);
				materialPropertyBlock.SetTexture(ShaderID._Mask, maskBuffer);
				materialPropertyBlock.SetTexture(ShaderID._MaskDepth, maskDepthBuffer);
				materialPropertyBlock.SetFloat(ShaderID._InvertMask, invertMask ? 1 : 0);
				SetViewPortSize(ctx.cmd, materialPropertyBlock, rTHandle);
				HDUtils.DrawFullScreen(ctx.cmd, compositeMaterial, rTHandle, materialPropertyBlock);
			}
		}
	}

	protected override void Cleanup()
	{
		CoreUtils.Destroy(compositeMaterial);
		CoreUtils.Destroy(whiteRenderersMaterial);
		downSampleBuffer.Release();
		blurBuffer.Release();
		maskDepthBuffer?.Release();
		maskBuffer?.Release();
		colorCopy?.Release();
	}
}
