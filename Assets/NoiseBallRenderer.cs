using UnityEngine;
using UnityEngine.Rendering;
using Unity.Collections;
using Unity.Mathematics;
using Random = Unity.Mathematics.Random;

public sealed class NoiseBallRenderer : MonoBehaviour
{
    [SerializeField] uint _triangleCount = 100;
    [SerializeField] float _extent = 0.5f;
    [SerializeField] float _noiseFrequency = 2.0f;
    [SerializeField] float _noiseAmplitude = 0.1f;
    [SerializeField] float3 _noiseSpeed = math.float3(0.1f, 0.2f, 0.3f);
    [SerializeField] uint _randomSeed = 123;

    [SerializeField] Material _material = null;

    Mesh _mesh;
    MeshFilter _meshFilter;
    MeshRenderer _meshRenderer;

    NativeArray<uint> _indexArray;
    NativeArray<float3> _vertexArray;

    Random _random;

    void Start()
    {
        InitializeVertexArray();
        InitializeIndexArray();

        BuildMesh();

        _meshFilter = gameObject.AddComponent<MeshFilter>();
        _meshFilter.sharedMesh = _mesh;

        _meshRenderer = gameObject.AddComponent<MeshRenderer>();
        _meshRenderer.sharedMaterial = _material;
    }

    void OnDestroy()
    {
        if (_indexArray.IsCreated) _indexArray.Dispose();
        if (_vertexArray.IsCreated) _vertexArray.Dispose();
        if (_mesh != null) Destroy(_mesh);
    }

    void Update()
    {
        UpdateVertexArray();
        UpdateMesh();
    }

    void BuildMesh()
    {
        var vcount = (int)_triangleCount * 3;

        _mesh = new Mesh();

        _mesh.SetVertexBufferParams(
            vcount,
            new VertexAttributeDescriptor
                (VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
            new VertexAttributeDescriptor
                (VertexAttribute.Normal, VertexAttributeFormat.Float32, 3)
        );

        _mesh.SetVertexBufferData(_vertexArray, 0, 0, vcount);

        _mesh.SetIndexBufferParams(vcount, IndexFormat.UInt32);
        _mesh.SetIndexBufferData(_indexArray, 0, 0, vcount);

        _mesh.SetSubMesh(
            0,
            new SubMeshDescriptor(0, vcount)
                { bounds = new Bounds(Vector3.zero, Vector3.one * 1000) }
        );
    }

    void UpdateMesh()
    {
        var vcount = (int)_triangleCount * 3;
        _mesh.SetVertexBufferData(_vertexArray, 0, 0, vcount);
        _mesh.SetIndexBufferData(_indexArray, 0, 0, vcount);
    }

    float3 RandomPoint()
    {
        var u = _random.NextFloat(math.PI * 2);
        var z = _random.NextFloat(-1, 1);
        var xy = math.sqrt(1 - z * z);
        return math.float3(math.cos(u) * xy, math.sin(u) * xy, z);
    }

    void InitializeVertexArray()
    {
        var vcount = (int)_triangleCount * 3;

        _vertexArray = new NativeArray<float3>(
            vcount * 2, Allocator.Persistent,
            NativeArrayOptions.UninitializedMemory
        );

        UpdateVertexArray();
    }

    void UpdateVertexArray()
    {
        _random = new Random(_randomSeed);

        var vcount = (int)_triangleCount * 3;

        var noffs = _noiseSpeed * Time.time;

        for (var i = 0; i < vcount * 2; i += 6)
        {
            var v1 = RandomPoint();
            var v2 = RandomPoint();
            var v3 = RandomPoint();

            v2 = math.normalize(v1 + math.normalize(v2 - v1) * _extent);
            v3 = math.normalize(v1 + math.normalize(v3 - v1) * _extent);

            var l1 = noise.snoise(v1 * _noiseFrequency + noffs);
            var l2 = noise.snoise(v2 * _noiseFrequency + noffs);
            var l3 = noise.snoise(v3 * _noiseFrequency + noffs);

            l1 = math.abs(l1 * l1 * l1);
            l2 = math.abs(l2 * l2 * l2);
            l3 = math.abs(l3 * l3 * l3);

            v1 *= 1 + l1 * _noiseAmplitude;
            v2 *= 1 + l2 * _noiseAmplitude;
            v3 *= 1 + l3 * _noiseAmplitude;

            var n = math.cross(v2 - v1, v3 - v1);

            _vertexArray[i + 0] = v1;
            _vertexArray[i + 1] = n;
            _vertexArray[i + 2] = v2;
            _vertexArray[i + 3] = n;
            _vertexArray[i + 4] = v3;
            _vertexArray[i + 5] = n;
        }
    }

    void InitializeIndexArray()
    {
        var vcount = (int)_triangleCount * 3;

        _indexArray = new NativeArray<uint>(
            vcount, Allocator.Persistent,
            NativeArrayOptions.UninitializedMemory
        );

        for (var i = 0; i < vcount; i++) _indexArray[i] = (uint)i;
    }
}
