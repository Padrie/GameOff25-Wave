using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshSeparator : MonoBehaviour
{
    [Header("Target Settings")]
    public GameObject targetObject;

    [Header("Settings")]
    public bool createSeparateObjects = true;
    public Transform separatedObjectsParent;
    public bool separateOnStart = false;
    public bool addMeshColliders = false;
    public bool makeCollidersConvex = true;
    public int maxColliderVertices = 255;

    private void Start()
    {
        if (separateOnStart)
            SeparateLooseParts();
    }

    public void SeparateLooseParts()
    {
        GameObject target = targetObject != null ? targetObject : gameObject;

        MeshFilter meshFilter = target.GetComponent<MeshFilter>();
        if (!meshFilter || !meshFilter.sharedMesh)
        {
            Debug.LogWarning("MeshSeparator: No MeshFilter or mesh found on target object!");
            return;
        }

        Mesh originalMesh = meshFilter.sharedMesh;
        List<List<int>> looseParts = DetectLooseParts(originalMesh);

        Debug.Log($"MeshSeparator: Found {looseParts.Count} loose part(s)");

        if (createSeparateObjects && looseParts.Count > 1)
            CreateSeparateGameObjects(target, originalMesh, looseParts);
    }

    private List<List<int>> DetectLooseParts(Mesh mesh)
    {
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        int vertexCount = mesh.vertexCount;
        int triangleCount = triangles.Length / 3;

        Dictionary<long, List<int>> edgeToTriangles = new Dictionary<long, List<int>>();
        List<int>[] vertexToTriangles = new List<int>[vertexCount];

        for (int triIndex = 0; triIndex < triangleCount; triIndex++)
        {
            int baseIndex = triIndex * 3;
            int v0 = triangles[baseIndex];
            int v1 = triangles[baseIndex + 1];
            int v2 = triangles[baseIndex + 2];

            AddEdge(edgeToTriangles, v0, v1, triIndex);
            AddEdge(edgeToTriangles, v1, v2, triIndex);
            AddEdge(edgeToTriangles, v2, v0, triIndex);

            if (vertexToTriangles[v0] == null) vertexToTriangles[v0] = new List<int>();
            if (vertexToTriangles[v1] == null) vertexToTriangles[v1] = new List<int>();
            if (vertexToTriangles[v2] == null) vertexToTriangles[v2] = new List<int>();

            vertexToTriangles[v0].Add(triIndex);
            vertexToTriangles[v1].Add(triIndex);
            vertexToTriangles[v2].Add(triIndex);
        }

        bool[] visited = new bool[triangleCount];
        List<List<int>> looseParts = new List<List<int>>();
        Queue<int> queue = new Queue<int>();

        for (int startTri = 0; startTri < triangleCount; startTri++)
        {
            if (visited[startTri])
                continue;

            queue.Clear();
            List<int> part = new List<int>();

            queue.Enqueue(startTri);
            visited[startTri] = true;

            while (queue.Count > 0)
            {
                int tri = queue.Dequeue();
                part.Add(tri);

                int baseIndex = tri * 3;
                int v0 = triangles[baseIndex];
                int v1 = triangles[baseIndex + 1];
                int v2 = triangles[baseIndex + 2];

                EnqueueAdjacentTriangles(edgeToTriangles, v0, v1, visited, queue);
                EnqueueAdjacentTriangles(edgeToTriangles, v1, v2, visited, queue);
                EnqueueAdjacentTriangles(edgeToTriangles, v2, v0, visited, queue);

                EnqueueAdjacentByVertex(vertexToTriangles, v0, visited, queue);
                EnqueueAdjacentByVertex(vertexToTriangles, v1, visited, queue);
                EnqueueAdjacentByVertex(vertexToTriangles, v2, visited, queue);

                EnqueueAdjacentByPosition(vertices, v0, visited, queue, triangles);
                EnqueueAdjacentByPosition(vertices, v1, visited, queue, triangles);
                EnqueueAdjacentByPosition(vertices, v2, visited, queue, triangles);
            }

            looseParts.Add(part);
        }

        return looseParts;
    }

    private long GetEdgeKey(int a, int b)
    {
        if (a > b) (a, b) = (b, a);
        return ((long)a << 32) | (uint)b;
    }

    private void AddEdge(Dictionary<long, List<int>> map, int a, int b, int tri)
    {
        long k = GetEdgeKey(a, b);
        if (!map.TryGetValue(k, out var list))
            map[k] = list = new List<int>();
        list.Add(tri);
    }

    private void EnqueueAdjacentTriangles(Dictionary<long, List<int>> map, int a, int b, bool[] visited, Queue<int> queue)
    {
        if (map.TryGetValue(GetEdgeKey(a, b), out var tris))
        {
            foreach (int t in tris)
            {
                if (!visited[t])
                {
                    visited[t] = true;
                    queue.Enqueue(t);
                }
            }
        }
    }

    private void EnqueueAdjacentByVertex(List<int>[] map, int v, bool[] visited, Queue<int> queue)
    {
        var list = map[v];
        if (list == null) return;
        foreach (int t in list)
        {
            if (!visited[t])
            {
                visited[t] = true;
                queue.Enqueue(t);
            }
        }
    }

    private void EnqueueAdjacentByPosition(Vector3[] verts, int v, bool[] visited, Queue<int> queue, int[] triArray, float threshold = 0.0001f)
    {
        Vector3 pos = verts[v];
        int triCount = triArray.Length / 3;

        for (int tri = 0; tri < triCount; tri++)
        {
            if (visited[tri])
                continue;

            int baseIndex = tri * 3;
            int a = triArray[baseIndex];
            int b = triArray[baseIndex + 1];
            int c = triArray[baseIndex + 2];

            if (Vector3.Distance(verts[a], pos) < threshold ||
                Vector3.Distance(verts[b], pos) < threshold ||
                Vector3.Distance(verts[c], pos) < threshold)
            {
                visited[tri] = true;
                queue.Enqueue(tri);
            }
        }
    }

    private void ClearChildren(Transform parent)
    {
        if (!parent) return;

        List<GameObject> toDestroy = new List<GameObject>();
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child != transform)
                toDestroy.Add(child.gameObject);
        }

        foreach (var go in toDestroy)
            if (Application.isPlaying) Destroy(go); else DestroyImmediate(go);
    }

    private void CreateSeparateGameObjects(GameObject target, Mesh mesh, List<List<int>> looseParts)
    {
        MeshRenderer originalRenderer = target.GetComponent<MeshRenderer>();
        Material[] materials = originalRenderer?.sharedMaterials;

        MeshCollider originalCollider = target.GetComponent<MeshCollider>();
        PhysicsMaterial physicsMat = originalCollider ? originalCollider.sharedMaterial : null;

        int originalLayer = target.layer;
        Transform parent = separatedObjectsParent != null ? separatedObjectsParent : target.transform.parent;

        ClearChildren(parent);

        int created = 0;

        for (int i = 0; i < looseParts.Count; i++)
        {
            Mesh newMesh = CreateMeshFromTriangles(mesh, looseParts[i]);
            if (!newMesh)
                continue;

            GameObject part = new GameObject($"{target.name}_Part_{i + 1}");
            part.transform.SetParent(parent);
            part.transform.position = target.transform.position;
            part.transform.rotation = target.transform.rotation;
            part.transform.localScale = Vector3.one;
            part.isStatic = target.isStatic;
            part.layer = originalLayer;

            MeshFilter mf = part.AddComponent<MeshFilter>();
            mf.sharedMesh = newMesh;

            MeshRenderer mr = part.AddComponent<MeshRenderer>();
            CopyMeshRendererParameters(originalRenderer, mr, materials);

            Vector3 center = newMesh.bounds.center;
            part.transform.position += part.transform.rotation * center;

            Vector3[] verts = newMesh.vertices;
            for (int v = 0; v < verts.Length; v++)
                verts[v] -= center;
            newMesh.vertices = verts;
            newMesh.RecalculateBounds();

            mf.sharedMesh = newMesh;

            if (addMeshColliders && IsValidForCollider(newMesh))
            {
                MeshCollider mc = part.AddComponent<MeshCollider>();
                mc.sharedMesh = newMesh;
                mc.convex = makeCollidersConvex;
                if (physicsMat) mc.sharedMaterial = physicsMat;
            }

            created++;
        }

        Debug.Log($"MeshSeparator: Created {created} separate objects");

        target.SetActive(false);
    }

    private void CopyMeshRendererParameters(MeshRenderer src, MeshRenderer dst, Material[] mats)
    {
        if (mats != null && mats.Length > 0)
            dst.sharedMaterials = mats;

        if (!src)
            return;

        dst.enabled = src.enabled;
        dst.shadowCastingMode = src.shadowCastingMode;
        dst.receiveShadows = src.receiveShadows;
        dst.motionVectorGenerationMode = src.motionVectorGenerationMode;
        dst.lightProbeUsage = src.lightProbeUsage;

        if (src.lightProbeUsage == LightProbeUsage.UseProxyVolume)
            dst.lightProbeProxyVolumeOverride = src.lightProbeProxyVolumeOverride;

        dst.reflectionProbeUsage = src.reflectionProbeUsage;
        dst.renderingLayerMask = src.renderingLayerMask;
        dst.sortingLayerID = src.sortingLayerID;
        dst.sortingOrder = src.sortingOrder;
        dst.allowOcclusionWhenDynamic = src.allowOcclusionWhenDynamic;
    }

    private Mesh CreateMeshFromTriangles(Mesh original, List<int> tris)
    {
        int[] originalTris = original.triangles;
        Vector3[] originalVerts = original.vertices;
        Vector3[] originalNormals = original.normals;
        Vector2[] originalUV = original.uv;
        Vector2[] originalUV2 = original.uv2;
        Color[] originalColors = original.colors;

        Dictionary<int, int> map = new Dictionary<int, int>();
        List<Vector3> verts = new List<Vector3>();
        List<Vector3> normals = new List<Vector3>();
        List<Vector2> uv = new List<Vector2>();
        List<Vector2> uv2 = new List<Vector2>();
        List<Color> colors = new List<Color>();
        List<int> newTris = new List<int>();

        foreach (int t in tris)
        {
            int b = t * 3;
            int a0 = originalTris[b];
            int a1 = originalTris[b + 1];
            int a2 = originalTris[b + 2];

            int n0 = GetOrCreateVertex(a0);
            int n1 = GetOrCreateVertex(a1);
            int n2 = GetOrCreateVertex(a2);

            newTris.Add(n0);
            newTris.Add(n1);
            newTris.Add(n2);
        }

        int GetOrCreateVertex(int old)
        {
            if (map.TryGetValue(old, out int v))
                return v;

            int newIndex = verts.Count;
            map[old] = newIndex;

            verts.Add(originalVerts[old]);
            if (originalNormals != null && old < originalNormals.Length) normals.Add(originalNormals[old]);
            if (originalUV != null && old < originalUV.Length) uv.Add(originalUV[old]);
            if (originalUV2 != null && old < originalUV2.Length) uv2.Add(originalUV2[old]);
            if (originalColors != null && old < originalColors.Length) colors.Add(originalColors[old]);

            return newIndex;
        }

        if (verts.Count == 0 || newTris.Count == 0)
            return null;

        Mesh m = new Mesh();
        m.name = $"SeparatedPart_{newTris.Count / 3}tris";
        m.SetVertices(verts);

        if (normals.Count > 0) m.SetNormals(normals);
        if (uv.Count > 0) m.SetUVs(0, uv);
        if (uv2.Count > 0) m.SetUVs(1, uv2);
        if (colors.Count > 0) m.SetColors(colors);

        m.SetTriangles(newTris, 0);

        if (normals.Count == 0)
            m.RecalculateNormals();

        m.RecalculateBounds();
        return m;
    }

    private bool IsValidForCollider(Mesh m)
    {
        if (!m || m.vertexCount == 0 || m.triangles.Length == 0)
            return false;

        if (maxColliderVertices > 0 && m.vertexCount > maxColliderVertices)
        {
            Debug.LogWarning($"MeshSeparator: Mesh has {m.vertexCount} vertices, exceeds limit of {maxColliderVertices}. Skipping collider.");
            return false;
        }

        return true;
    }

    public int GetLoosePartCount()
    {
        GameObject target = targetObject != null ? targetObject : gameObject;

        MeshFilter mf = target.GetComponent<MeshFilter>();
        if (!mf || !mf.sharedMesh)
            return 0;

        return DetectLooseParts(mf.sharedMesh).Count;
    }
}
