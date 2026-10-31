using Archura.WIS.Core;
using UnityEngine;

namespace Archura.WIS.Responders
{
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class WaterResponder : InteractionResponder
    {
        private struct Ripple
        {
            public Vector2 UV;
            public float StartTime;
            public float Radius;
            public float Strength;
            public Vector3 Direction;
        }

        private const int MaxRipples = 96;
        private const int ThreadGroupSize = 8;

        private static readonly int RippleTextureId = Shader.PropertyToID("_RippleTex");
        private static readonly int ResolutionId = Shader.PropertyToID("_Resolution");
        private static readonly int RippleCountId = Shader.PropertyToID("_RippleCount");
        private static readonly int WorldUAxisId = Shader.PropertyToID("_WorldUAxis");
        private static readonly int WorldVAxisId = Shader.PropertyToID("_WorldVAxis");
        private static readonly int WaveSpeedId = Shader.PropertyToID("_WaveSpeed");
        private static readonly int DampingId = Shader.PropertyToID("_Damping");
        private static readonly int WakeWaveSpeedId = Shader.PropertyToID("_WakeWaveSpeed");
        private static readonly int WakeDampingId = Shader.PropertyToID("_WakeDamping");
        private static readonly int BlendStrengthId = Shader.PropertyToID("_BlendStrength");
        private static readonly int RipplesId = Shader.PropertyToID("_Ripples");
        private static readonly int StrengthsId = Shader.PropertyToID("_Strengths");

        [Header("References")]
        [SerializeField] private ComputeShader rippleShader;

        [Header("General Settings")]
        [SerializeField, Range(64, 512)] private int resolution = 256;
        [SerializeField, Range(1, MaxRipples)] private int maxRippleCount = 48;
        [SerializeField, Range(0f, 1f)] private float blendStrength = 0.6f;

        [Header("Ripple Settings")]
        [SerializeField, Min(0.1f)] private float waveSpeed = 3f;
        [SerializeField, Min(0f)] private float damping = 1f;
        [SerializeField, Min(0.1f)] private float lifetime = 4f;
        [SerializeField, Range(0f, 1f)] private float rippleStrength = 1f;

        [Header("Wake Settings")]
        [SerializeField, Min(0.1f)] private float wakeWaveSpeed = 2f;
        [SerializeField, Min(0f)] private float wakeDamping = 1.5f;
        [SerializeField, Min(0.1f)] private float wakeLifetime = 1.75f;
        [SerializeField, Range(0f, 1f)] private float wakeStrength = 0.4f;

        private readonly Ripple[] ripples = new Ripple[MaxRipples];
        private readonly Vector4[] rippleData = new Vector4[MaxRipples];
        private readonly Vector4[] rippleStrengths = new Vector4[MaxRipples];

        private MeshRenderer waterRenderer;
        private MaterialPropertyBlock propertyBlock;
        private RenderTexture rippleTexture;
        private PlanarSurfaceMapping surfaceMapping;

        private int kernel;
        private int nextRipple;
        private int rippleCount;
        private int lastRenderedCount;
        private int rippleCapacity;

        private void OnEnable()
        {
            MeshFilter meshFilter = GetComponent<MeshFilter>();
            waterRenderer = GetComponent<MeshRenderer>();

            if (!ValidateSetup(meshFilter))
            {
                enabled = false;
                return;
            }

            kernel = rippleShader.FindKernel("CSMain");
            rippleTexture = CreateRippleTexture();

            if (!rippleTexture.IsCreated())
            {
                Debug.LogError("WaterResponder could not create the ripple RenderTexture.", this);
                enabled = false;
                return;
            }

            propertyBlock = new MaterialPropertyBlock();
            waterRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetTexture(RippleTextureId, rippleTexture);
            waterRenderer.SetPropertyBlock(propertyBlock);

            rippleCapacity = Mathf.Clamp(maxRippleCount, 1, MaxRipples);
            nextRipple = 0;
            rippleCount = 0;
            lastRenderedCount = -1;
        }

        protected override void OnInteraction(InteractionSurface surface, InteractionChannel channel, in InteractionSample sample)
        {
            if (rippleTexture == null || sample.Strength <= 0f)
                return;

            if (!surfaceMapping.TryWorldToUV(sample.Position, out Vector2 uv))
                return;

            ripples[nextRipple] = new Ripple
            {
                UV = uv,
                StartTime = Time.time,
                Radius = Mathf.Max(0.05f, sample.Radius),
                Strength = sample.Strength,
                Direction = Vector3.ProjectOnPlane(sample.Velocity, transform.up).normalized
            };

            nextRipple = (nextRipple + 1) % rippleCapacity;
            rippleCount = Mathf.Min(rippleCount + 1, rippleCapacity);
        }

        private void Update()
        {
            if (rippleTexture == null)
                return;

            UpdateCapacity();

            int activeCount = GetActiveRipples();

            if (activeCount == 0 && lastRenderedCount == 0)
                return;

            RenderRipples(activeCount);
            lastRenderedCount = activeCount;
        }

        private void UpdateCapacity()
        {
            int capacity = Mathf.Clamp(maxRippleCount, 1, MaxRipples);

            if (capacity == rippleCapacity)
                return;

            rippleCapacity = capacity;
            nextRipple = 0;
            rippleCount = 0;
            lastRenderedCount = -1;
        }

        private bool ValidateSetup(MeshFilter meshFilter)
        {
            if (rippleShader == null || meshFilter.sharedMesh == null ||
                !SystemInfo.supportsComputeShaders ||
                !SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.RHalf))
            {
                Debug.LogError("WaterResponder requires a mesh, compute shader and RHalf compute support.", this);
                return false;
            }

            if (!PlanarSurfaceMapping.TryCreate(meshFilter.sharedMesh, transform, out surfaceMapping))
            {
                Debug.LogError(
                    "WaterResponder requires a readable XZ-oriented plane with continuous UVs in the 0-1 range.", this);
                return false;
            }

            return true;
        }

        private RenderTexture CreateRippleTexture()
        {
            RenderTexture texture = new RenderTexture(
                resolution, resolution, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear)
            {
                enableRandomWrite = true,
                useMipMap = false,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            texture.Create();
            return texture;
        }

        private int GetActiveRipples()
        {
            int activeCount = 0;
            float currentTime = Time.time;

            for (int i = 0; i < rippleCount; i++)
            {
                Ripple ripple = ripples[i];
                float age = currentTime - ripple.StartTime;

                bool isWake = ripple.Direction.sqrMagnitude > 0.5f;
                float duration = isWake ? wakeLifetime : lifetime;

                if (age > duration)
                    continue;

                float intensity = ripple.Strength * (isWake ? wakeStrength : rippleStrength);

                rippleData[activeCount] = new Vector4(
                    ripple.UV.x, ripple.UV.y, age, ripple.Radius);

                rippleStrengths[activeCount] = new Vector4(
                    intensity, ripple.Direction.x, ripple.Direction.y, ripple.Direction.z);

                activeCount++;
            }

            return activeCount;
        }

        private void RenderRipples(int activeCount)
        {
            surfaceMapping.GetWorldUvAxes(out Vector3 uAxis, out Vector3 vAxis);

            rippleShader.SetInt(ResolutionId, resolution);
            rippleShader.SetInt(RippleCountId, activeCount);
            rippleShader.SetVector(WorldUAxisId, uAxis);
            rippleShader.SetVector(WorldVAxisId, vAxis);

            rippleShader.SetFloat(WaveSpeedId, waveSpeed);
            rippleShader.SetFloat(DampingId, damping);
            rippleShader.SetFloat(WakeWaveSpeedId, wakeWaveSpeed);
            rippleShader.SetFloat(WakeDampingId, wakeDamping);
            rippleShader.SetFloat(BlendStrengthId, blendStrength);

            rippleShader.SetVectorArray(RipplesId, rippleData);
            rippleShader.SetVectorArray(StrengthsId, rippleStrengths);
            rippleShader.SetTexture(kernel, RippleTextureId, rippleTexture);

            int groupCount = (resolution + ThreadGroupSize - 1) / ThreadGroupSize;
            rippleShader.Dispatch(kernel, groupCount, groupCount, 1);
        }

        private void OnDisable()
        {
            if (waterRenderer != null && propertyBlock != null)
            {
                waterRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetTexture(RippleTextureId, null);
                waterRenderer.SetPropertyBlock(propertyBlock);
            }

            if (rippleTexture == null)
                return;

            rippleTexture.Release();
            Destroy(rippleTexture);
            rippleTexture = null;
        }
    }
}
