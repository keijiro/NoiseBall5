using UnityEngine;
using UnityEngine.Rendering;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

public sealed class NoiseBallRenderer : MonoBehaviour
{
    #region Editable attributes

    [SerializeField, Range(0, 60000)] uint _triangleCount = 100;
    [SerializeField, Range(0, 2)] float _extent = 0.5f;
    [SerializeField, Range(0, 20)] float _noiseFrequency = 2.0f;
    [SerializeField, Range(0, 5)] float _noiseAmplitude = 0.1f;
    [SerializeField] float3 _noiseAnimation = math.float3(0.1f, 0.2f, 0.3f);
    [SerializeField] uint _randomSeed = 100;
    [SerializeField] Material _material = null;

    #endregion

    #region Local objects

    MeshFilter _meshFilter;
    MeshRenderer _meshRenderer;

    Mesh _mesh;
    NativeArray<uint> _indexBuffer;
    NativeArray<float3> _vertexBuffer;

    #endregion

    #region MonoBehaviour implementation

    void Start()
    {
        _mesh = new Mesh();

        _meshFilter = gameObject.AddComponent<MeshFilter>();
        _meshFilter.sharedMesh = _mesh;

        _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        _meshRenderer.sharedMaterial = _material;
    }

    void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
        DisposeBuffers();
    }

    void Update()
    {
        if (_indexBuffer.Length != VertexCount)
        {
            // The vertex count was changed, or this is the first update.

            // Dispose the current mesh.
            _mesh.Clear();
            DisposeBuffers();

            // Mesh reallocation and reconstruction
            AllocateBuffers();
            UpdateVertexBuffer();
            InitializeMesh();
        }
        else
        {
            // Only update the vertex data.
            UpdateVertexBuffer();
            UpdateMesh();
        }
    }

    #endregion

    #region Private properties and methods

    int VertexCount { get { return (int)_triangleCount * 3; } }

    #endregion

    #region Index/vertex buffer operations

    void AllocateBuffers()
    {
        _indexBuffer = new NativeArray<uint>(
            VertexCount, Allocator.Persistent,
            NativeArrayOptions.UninitializedMemory
        );

        _vertexBuffer = new NativeArray<float3>(
            VertexCount * 2, Allocator.Persistent,
            NativeArrayOptions.UninitializedMemory
        );

        // Index array initialization
        for (var i = 0; i < VertexCount; i++) _indexBuffer[i] = (uint)i;
    }

    void DisposeBuffers()
    {
        if (_indexBuffer.IsCreated) _indexBuffer.Dispose();
        if (_vertexBuffer.IsCreated) _vertexBuffer.Dispose();
    }

    #endregion

    #region Mesh object operations

    void InitializeMesh()
    {
        _mesh.SetVertexBufferParams(
            VertexCount,
            new VertexAttributeDescriptor
                (VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor
                (VertexAttribute.Normal, VertexAttributeFormat.Float32, 3)
        );
        _mesh.SetVertexBufferData(_vertexBuffer, 0, 0, VertexCount * 2);

        _mesh.SetIndexBufferParams(VertexCount, IndexFormat.UInt32);
        _mesh.SetIndexBufferData(_indexBuffer, 0, 0, VertexCount);

        _mesh.SetSubMesh(0, new SubMeshDescriptor(0, VertexCount));
        _mesh.bounds = new Bounds(Vector3.zero, Vector3.one * 1000);
    }

    void UpdateMesh()
    {
        _mesh.SetVertexBufferData(_vertexBuffer, 0, 0, VertexCount * 2);
    }

    #endregion

    #region Jobified vertex animation

    void UpdateVertexBuffer()
    {
        var job = new VertexUpdateJob{
            seed = _randomSeed,
            extent = _extent,
            noiseFrequency = _noiseFrequency,
            noiseOffset = _noiseAnimation * Time.time,
            noiseAmplitude = _noiseAmplitude,
            buffer = _vertexBuffer
        };

        job.Schedule((int)_triangleCount, 64).Complete();
    }

    [BurstCompile(CompileSynchronously = true,
        FloatMode = FloatMode.Fast, FloatPrecision = FloatPrecision.Low)]
    struct VertexUpdateJob : IJobParallelFor
    {
        [ReadOnly] public uint seed;
        [ReadOnly] public float extent;
        [ReadOnly] public float noiseFrequency;
        [ReadOnly] public float3 noiseOffset;
        [ReadOnly] public float noiseAmplitude;

        [NativeDisableParallelForRestriction]
        [WriteOnly] public NativeArray<float3> buffer;

        Random _random;

        float3 RandomPoint()
        {
            var u = _random.NextFloat(math.PI * 2);
            var z = _random.NextFloat(-1, 1);
            var xy = math.sqrt(1 - z * z);
            return math.float3(math.cos(u) * xy, math.sin(u) * xy, z);
        }

        public void Execute(int i)
        {
            _random = new Random(seed + (uint)i * 10);
            _random.NextInt();

            var v1 = RandomPoint();
            var v2 = RandomPoint();
            var v3 = RandomPoint();

            v2 = math.normalize(v1 + math.normalize(v2 - v1) * extent);
            v3 = math.normalize(v1 + math.normalize(v3 - v1) * extent);

            var l1 = noise.snoise(v1 * noiseFrequency + noiseOffset);
            var l2 = noise.snoise(v2 * noiseFrequency + noiseOffset);
            var l3 = noise.snoise(v3 * noiseFrequency + noiseOffset);

            l1 = math.abs(l1 * l1 * l1);
            l2 = math.abs(l2 * l2 * l2);
            l3 = math.abs(l3 * l3 * l3);

            v1 *= 1 + l1 * noiseAmplitude;
            v2 *= 1 + l2 * noiseAmplitude;
            v3 *= 1 + l3 * noiseAmplitude;

            var n = math.cross(v2 - v1, v3 - v1);

            var offs = i * 6;

            buffer[offs++] = v1;
            buffer[offs++] = n;
            buffer[offs++] = v2;
            buffer[offs++] = n;
            buffer[offs++] = v3;
            buffer[offs++] = n;
        }
    }

    #endregion
}
