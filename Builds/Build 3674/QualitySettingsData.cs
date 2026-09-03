using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Rendering;

public class QualitySettingsData : ScriptableObject
{
	[ReadOnly]
	public QualityLevel currentLevel;

	[ReadOnly]
	public int pixelLightCount;

	[ReadOnly]
	public ShadowQuality shadows;

	[ReadOnly]
	public ShadowProjection shadowProjection;

	[ReadOnly]
	public int shadowCascades;

	[ReadOnly]
	public float shadowDistance;

	[ReadOnly]
	public ShadowResolution shadowResolution;

	[ReadOnly]
	public ShadowmaskMode shadowmaskMode;

	[ReadOnly]
	public float shadowNearPlaneOffset;

	[ReadOnly]
	public float shadowCascade2Split;

	[ReadOnly]
	public Vector3 shadowCascade4Split;

	[ReadOnly]
	public float lodBias;

	[ReadOnly]
	public AnisotropicFiltering anisotropicFiltering;

	[ReadOnly]
	public int masterTextureLimit;

	[ReadOnly]
	public int globalTextureMipmapLimit;

	[ReadOnly]
	public int maximumLODLevel;

	[ReadOnly]
	public bool enableLODCrossFade;

	[ReadOnly]
	public int particleRaycastBudget;

	[ReadOnly]
	public bool softParticles;

	[ReadOnly]
	public bool softVegetation;

	[ReadOnly]
	public int vSyncCount;

	[ReadOnly]
	public int realtimeGICPUUsage;

	[ReadOnly]
	public int antiAliasing;

	[ReadOnly]
	public int asyncUploadTimeSlice;

	[ReadOnly]
	public int asyncUploadBufferSize;

	[ReadOnly]
	public bool asyncUploadPersistentBuffer;

	[ReadOnly]
	public bool realtimeReflectionProbes;

	[ReadOnly]
	public bool billboardsFaceCameraPosition;

	[ReadOnly]
	public bool useLegacyDetailDistribution;

	[ReadOnly]
	public float resolutionScalingFixedDPIFactor;

	[ReadOnly]
	public TerrainQualityOverrides terrainQualityOverrides;

	[ReadOnly]
	public float terrainPixelError;

	[ReadOnly]
	public float terrainDetailDensityScale;

	[ReadOnly]
	public float terrainBasemapDistance;

	[ReadOnly]
	public float terrainDetailDistance;

	[ReadOnly]
	public float terrainTreeDistance;

	[ReadOnly]
	public float terrainBillboardStart;

	[ReadOnly]
	public float terrainFadeLength;

	[ReadOnly]
	public float terrainMaxTrees;

	[ReadOnly]
	public RenderPipelineAsset renderPipeline;

	[ReadOnly]
	public SkinWeights blendWeights;

	[ReadOnly]
	public SkinWeights skinWeights;

	[ReadOnly]
	public bool streamingMipmapsActive;

	[ReadOnly]
	public float streamingMipmapsMemoryBudget;

	[ReadOnly]
	public int streamingMipmapsRenderersPerFrame;

	[ReadOnly]
	public int streamingMipmapsMaxLevelReduction;

	[ReadOnly]
	public bool streamingMipmapsAddAllCameras;

	[ReadOnly]
	public int streamingMipmapsMaxFileIORequests;

	[ReadOnly]
	public int maxQueuedFrames;

	public void ReadFromQualitySettings()
	{
		currentLevel = QualitySettings.currentLevel;
		pixelLightCount = QualitySettings.pixelLightCount;
		shadows = QualitySettings.shadows;
		shadowProjection = QualitySettings.shadowProjection;
		shadowCascades = QualitySettings.shadowCascades;
		shadowDistance = QualitySettings.shadowDistance;
		shadowResolution = QualitySettings.shadowResolution;
		shadowmaskMode = QualitySettings.shadowmaskMode;
		shadowNearPlaneOffset = QualitySettings.shadowNearPlaneOffset;
		shadowCascade2Split = QualitySettings.shadowCascade2Split;
		shadowCascade4Split = QualitySettings.shadowCascade4Split;
		lodBias = QualitySettings.lodBias;
		anisotropicFiltering = QualitySettings.anisotropicFiltering;
		masterTextureLimit = QualitySettings.globalTextureMipmapLimit;
		globalTextureMipmapLimit = QualitySettings.globalTextureMipmapLimit;
		maximumLODLevel = QualitySettings.maximumLODLevel;
		enableLODCrossFade = QualitySettings.enableLODCrossFade;
		particleRaycastBudget = QualitySettings.particleRaycastBudget;
		softParticles = QualitySettings.softParticles;
		softVegetation = QualitySettings.softVegetation;
		vSyncCount = QualitySettings.vSyncCount;
		realtimeGICPUUsage = QualitySettings.realtimeGICPUUsage;
		antiAliasing = QualitySettings.antiAliasing;
		asyncUploadTimeSlice = QualitySettings.asyncUploadTimeSlice;
		asyncUploadBufferSize = QualitySettings.asyncUploadBufferSize;
		asyncUploadPersistentBuffer = QualitySettings.asyncUploadPersistentBuffer;
		realtimeReflectionProbes = QualitySettings.realtimeReflectionProbes;
		billboardsFaceCameraPosition = QualitySettings.billboardsFaceCameraPosition;
		useLegacyDetailDistribution = QualitySettings.useLegacyDetailDistribution;
		resolutionScalingFixedDPIFactor = QualitySettings.resolutionScalingFixedDPIFactor;
		terrainQualityOverrides = QualitySettings.terrainQualityOverrides;
		terrainPixelError = QualitySettings.terrainPixelError;
		terrainDetailDensityScale = QualitySettings.terrainDetailDensityScale;
		terrainBasemapDistance = QualitySettings.terrainBasemapDistance;
		terrainDetailDistance = QualitySettings.terrainDetailDistance;
		terrainTreeDistance = QualitySettings.terrainTreeDistance;
		terrainBillboardStart = QualitySettings.terrainBillboardStart;
		terrainFadeLength = QualitySettings.terrainFadeLength;
		terrainMaxTrees = QualitySettings.terrainMaxTrees;
		renderPipeline = QualitySettings.renderPipeline;
		blendWeights = QualitySettings.skinWeights;
		skinWeights = QualitySettings.skinWeights;
		streamingMipmapsActive = QualitySettings.streamingMipmapsActive;
		streamingMipmapsMemoryBudget = QualitySettings.streamingMipmapsMemoryBudget;
		streamingMipmapsRenderersPerFrame = QualitySettings.streamingMipmapsRenderersPerFrame;
		streamingMipmapsMaxLevelReduction = QualitySettings.streamingMipmapsMaxLevelReduction;
		streamingMipmapsAddAllCameras = QualitySettings.streamingMipmapsAddAllCameras;
		streamingMipmapsMaxFileIORequests = QualitySettings.streamingMipmapsMaxFileIORequests;
		maxQueuedFrames = QualitySettings.maxQueuedFrames;
	}

	public void ApplyToQualitySettings()
	{
		QualitySettings.currentLevel = currentLevel;
		QualitySettings.pixelLightCount = pixelLightCount;
		QualitySettings.shadows = shadows;
		QualitySettings.shadowProjection = shadowProjection;
		QualitySettings.shadowCascades = shadowCascades;
		QualitySettings.shadowDistance = shadowDistance;
		QualitySettings.shadowResolution = shadowResolution;
		QualitySettings.shadowmaskMode = shadowmaskMode;
		QualitySettings.shadowNearPlaneOffset = shadowNearPlaneOffset;
		QualitySettings.shadowCascade2Split = shadowCascade2Split;
		QualitySettings.shadowCascade4Split = shadowCascade4Split;
		QualitySettings.lodBias = lodBias;
		QualitySettings.anisotropicFiltering = anisotropicFiltering;
		QualitySettings.globalTextureMipmapLimit = masterTextureLimit;
		QualitySettings.globalTextureMipmapLimit = globalTextureMipmapLimit;
		QualitySettings.maximumLODLevel = maximumLODLevel;
		QualitySettings.enableLODCrossFade = enableLODCrossFade;
		QualitySettings.particleRaycastBudget = particleRaycastBudget;
		QualitySettings.softParticles = softParticles;
		QualitySettings.softVegetation = softVegetation;
		QualitySettings.vSyncCount = vSyncCount;
		QualitySettings.realtimeGICPUUsage = realtimeGICPUUsage;
		QualitySettings.antiAliasing = antiAliasing;
		QualitySettings.asyncUploadTimeSlice = asyncUploadTimeSlice;
		QualitySettings.asyncUploadBufferSize = asyncUploadBufferSize;
		QualitySettings.asyncUploadPersistentBuffer = asyncUploadPersistentBuffer;
		QualitySettings.realtimeReflectionProbes = realtimeReflectionProbes;
		QualitySettings.billboardsFaceCameraPosition = billboardsFaceCameraPosition;
		QualitySettings.useLegacyDetailDistribution = useLegacyDetailDistribution;
		QualitySettings.resolutionScalingFixedDPIFactor = resolutionScalingFixedDPIFactor;
		QualitySettings.terrainQualityOverrides = terrainQualityOverrides;
		QualitySettings.terrainPixelError = terrainPixelError;
		QualitySettings.terrainDetailDensityScale = terrainDetailDensityScale;
		QualitySettings.terrainBasemapDistance = terrainBasemapDistance;
		QualitySettings.terrainDetailDistance = terrainDetailDistance;
		QualitySettings.terrainTreeDistance = terrainTreeDistance;
		QualitySettings.terrainBillboardStart = terrainBillboardStart;
		QualitySettings.terrainFadeLength = terrainFadeLength;
		QualitySettings.terrainMaxTrees = terrainMaxTrees;
		QualitySettings.renderPipeline = renderPipeline;
		QualitySettings.skinWeights = blendWeights;
		QualitySettings.skinWeights = skinWeights;
		QualitySettings.streamingMipmapsActive = streamingMipmapsActive;
		QualitySettings.streamingMipmapsMemoryBudget = streamingMipmapsMemoryBudget;
		QualitySettings.streamingMipmapsRenderersPerFrame = streamingMipmapsRenderersPerFrame;
		QualitySettings.streamingMipmapsMaxLevelReduction = streamingMipmapsMaxLevelReduction;
		QualitySettings.streamingMipmapsAddAllCameras = streamingMipmapsAddAllCameras;
		QualitySettings.streamingMipmapsMaxFileIORequests = streamingMipmapsMaxFileIORequests;
		QualitySettings.maxQueuedFrames = maxQueuedFrames;
	}
}
