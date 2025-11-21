using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class MeshSeparator : MonoBehaviour
{
    [Header("Target Settings")]
    [Tooltip("GameObject to separate. If null, uses the GameObject this script is attached to")]
    public GameObject targetObject;

    [Header("Settings")]
    [Tooltip("Create separate GameObjects for each loose part")]
    public bool createSeparateObjects = true;

    [Tooltip("Parent for separated objects")]
    public Transform separatedObjectsParent;

    [Tooltip("Run separation on Start")]
    public bool separateOnStart = false;

    [Tooltip("Add mesh colliders to separated parts")]
    public bool addMeshColliders = false;

    [Tooltip("Make colliders convex (recommended for small parts)")]
    public bool makeCollidersConvex = true;

    [Tooltip("Maximum vertices for mesh collider (0 = no limit)")]
    public int maxColliderVertices = 255;

    [Header("Degenerate Triangles")]
    [Tooltip("Squared area threshold for removing degenerate triangles (0 = disabled)")]
    public float degenerateTriangleSqrAreaThreshold = 1e-10f;

    private void Start()
    {
        if (separateOnStart)
        {
            SeparateLooseParts();
        }
    }

    public void SeparateLooseParts()
    {
        GameObject target = targetObject != null ? targetObject : gameObject;

        MeshFilter meshFilter = target.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning("MeshSeparator: No MeshFilter or mesh found on target object!");
            return;
        }

        Mesh originalMesh = meshFilter.sharedMesh;
        List<List<int>> looseParts = DetectLooseParts(originalMesh);

        Debug.Log($"MeshSeparator: Found {looseParts.Count} loose part(s)");

        if (createSeparateObjects && looseParts.Count > 1)
        {
            CreateSeparateGameObjects(target, originalMesh, looseParts);
        }
    }

    /// <summary>
    /// Detects loose parts in a mesh by analyzing triangle connectivity based on shared edges and vertices.
    /// Two triangles are considered connected if they share an edge OR share vertices (handles sharp angles).
    /// Also checks spatial proximity for split normals (duplicate vertices at same location).
    /// </summary>
    private List<List<int>> DetectLooseParts(Mesh mesh)
    {
        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;
        int vertexCount = mesh.vertexCount;
        int triangleCount = triangles.Length / 3;

        // Build edge -> list of triangle indices that contain this edge
        Dictionary<long, List<int>> edgeToTriangles = new Dictionary<long, List<int>>();

        // Build vertex -> list of triangle indices that contain this vertex
        List<int>[] vertexToTriangles = new List<int>[vertexCount];

        for (int triIndex = 0; triIndex < triangleCount; triIndex++)
        {
            int baseIndex = triIndex * 3;
            int v0 = triangles[baseIndex];
            int v1 = triangles[baseIndex + 1];
            int v2 = triangles[baseIndex + 2];

            // Add the three edges of this triangle
            AddEdge(edgeToTriangles, v0, v1, triIndex);
            AddEdge(edgeToTriangles, v1, v2, triIndex);
            AddEdge(edgeToTriangles, v2, v0, triIndex);

            // Add to vertex map
            if (vertexToTriangles[v0] == null) vertexToTriangles[v0] = new List<int>();
            if (vertexToTriangles[v1] == null) vertexToTriangles[v1] = new List<int>();
            if (vertexToTriangles[v2] == null) vertexToTriangles[v2] = new List<int>();

            vertexToTriangles[v0].Add(triIndex);
            vertexToTriangles[v1].Add(triIndex);
            vertexToTriangles[v2].Add(triIndex);
        }

        bool[] visitedTriangles = new bool[triangleCount];
        List<List<int>> looseParts = new List<List<int>>();
        Queue<int> queue = new Queue<int>();

        for (int startTri = 0; startTri < triangleCount; startTri++)
        {
            if (visitedTriangles[startTri])
                continue;

            queue.Clear();
            List<int> currentPart = new List<int>();

            queue.Enqueue(startTri);
            visitedTriangles[startTri] = true;

            while (queue.Count > 0)
            {
                int triIndex = queue.Dequeue();
                currentPart.Add(triIndex);

                int baseIndex = triIndex * 3;
                int v0 = triangles[baseIndex];
                int v1 = triangles[baseIndex + 1];
                int v2 = triangles[baseIndex + 2];

                // Check all three edges for adjacent triangles (edge-shared)
                EnqueueAdjacentTriangles(edgeToTriangles, v0, v1, triIndex, visitedTriangles, queue);
                EnqueueAdjacentTriangles(edgeToTriangles, v1, v2, triIndex, visitedTriangles, queue);
                EnqueueAdjacentTriangles(edgeToTriangles, v2, v0, triIndex, visitedTriangles, queue);

                // Also check triangles sharing vertices (for sharp angles and corners)
                EnqueueAdjacentByVertex(vertexToTriangles, v0, triIndex, visitedTriangles, queue);
                EnqueueAdjacentByVertex(vertexToTriangles, v1, triIndex, visitedTriangles, queue);
                EnqueueAdjacentByVertex(vertexToTriangles, v2, triIndex, visitedTriangles, queue);

                // Check by spatial position (handles split normals - duplicate vertices at same location)
                EnqueueAdjacentByPosition(vertices, v0, triIndex, visitedTriangles, queue, triangles);
                EnqueueAdjacentByPosition(vertices, v1, triIndex, visitedTriangles, queue, triangles);
                EnqueueAdjacentByPosition(vertices, v2, triIndex, visitedTriangles, queue, triangles);
            }

            looseParts.Add(currentPart);
        }

        return looseParts;
    }

    /// <summary>
    /// Creates a unique, order-independent key for an edge between two vertices.
    /// </summary>
    private long GetEdgeKey(int v0, int v1)
    {
        // Create a unique key for an edge (order-independent)
        if (v0 > v1) (v0, v1) = (v1, v0);
        return ((long)v0 << 32) | (uint)v1;
    }

    /// <summary>
    /// Adds an edge and associates it with a triangle index.
    /// </summary>
    private void AddEdge(Dictionary<long, List<int>> edgeToTriangles, int v0, int v1, int triIndex)
    {
        long key = GetEdgeKey(v0, v1);
        if (!edgeToTriangles.ContainsKey(key))
            edgeToTriangles[key] = new List<int>();
        edgeToTriangles[key].Add(triIndex);
    }

    /// <summary>
    /// Enqueues adjacent triangles that share an edge with the current triangle.
    /// </summary>
    private void EnqueueAdjacentTriangles(Dictionary<long, List<int>> edgeToTriangles, int v0, int v1, int currentTri, bool[] visitedTriangles, Queue<int> queue)
    {
        long key = GetEdgeKey(v0, v1);
        if (edgeToTriangles.TryGetValue(key, out List<int> adjacentTris))
        {
            foreach (int neighborTri in adjacentTris)
            {
                if (!visitedTriangles[neighborTri])
                {
                    visitedTriangles[neighborTri] = true;
                    queue.Enqueue(neighborTri);
                }
            }
        }
    }

    /// <summary>
    /// Enqueues adjacent triangles that share a vertex with the current triangle.
    /// This handles sharp angles where triangles might not share a full edge.
    /// </summary>
    private void EnqueueAdjacentByVertex(List<int>[] vertexToTriangles, int vertex, int currentTri, bool[] visitedTriangles, Queue<int> queue)
    {
        if (vertexToTriangles[vertex] != null)
        {
            foreach (int neighborTri in vertexToTriangles[vertex])
            {
                if (!visitedTriangles[neighborTri])
                {
                    visitedTriangles[neighborTri] = true;
                    queue.Enqueue(neighborTri);
                }
            }
        }
    }

    /// <summary>
    /// Enqueues triangles that have vertices close to the given position (handles split normals/hard edges).
    /// This is crucial for meshes with hard edges where duplicate vertices exist at the same location.
    /// </summary>
    private void EnqueueAdjacentByPosition(Vector3[] vertices, int vertex, int currentTri, bool[] visitedTriangles, Queue<int> queue, int[] triangles, float positionThreshold = 0.0001f)
    {
        Vector3 targetPos = vertices[vertex];
        int triangleCount = triangles.Length / 3;

        for (int triIndex = 0; triIndex < triangleCount; triIndex++)
        {
            if (visitedTriangles[triIndex])
                continue;

            int baseIndex = triIndex * 3;
            int v0 = triangles[baseIndex];
            int v1 = triangles[baseIndex + 1];
            int v2 = triangles[baseIndex + 2];

            // Check if any vertex of this triangle is at the same position
            if (Vector3.Distance(vertices[v0], targetPos) < positionThreshold ||
                Vector3.Distance(vertices[v1], targetPos) < positionThreshold ||
                Vector3.Distance(vertices[v2], targetPos) < positionThreshold)
            {
                visitedTriangles[triIndex] = true;
                queue.Enqueue(triIndex);
            }
        }
    }

    /// <summary>
    /// Deletes all children of a transform.
    /// </summary>
    private void ClearChildren(Transform parent)
    {
        if (parent == null)
            return;

        List<GameObject> childrenToDelete = new List<GameObject>();
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child != transform) // don't delete self
            {
                childrenToDelete.Add(child.gameObject);
            }
        }

        foreach (GameObject child in childrenToDelete)
        {
            if (Application.isPlaying)
                Destroy(child);
            else
                DestroyImmediate(child);
        }
    }

    private void CreateSeparateGameObjects(GameObject targetObject, Mesh originalMesh, List<List<int>> looseParts)
    {
        MeshRenderer originalRenderer = targetObject.GetComponent<MeshRenderer>();
        Material[] materials = originalRenderer?.sharedMaterials;

        // Get original collider and its physics material
        MeshCollider originalCollider = targetObject.GetComponent<MeshCollider>();
        PhysicsMaterial originalPhysicsMaterial = originalCollider != null
            ? originalCollider.sharedMaterial
            : null;

        // Get original layer
        int originalLayer = targetObject.layer;

        Transform parent = separatedObjectsParent != null ? separatedObjectsParent : targetObject.transform.parent;

        // Clear existing children before creating new objects
        ClearChildren(parent);

        int successfulParts = 0;

        for (int partIndex = 0; partIndex < looseParts.Count; partIndex++)
        {
            Mesh newMesh = CreateMeshFromTriangles(originalMesh, looseParts[partIndex]);

            // If all triangles were degenerate, skip this part
            if (newMesh == null || newMesh.triangles.Length == 0)
                continue;

            // Create new GameObject
            GameObject partObject = new GameObject($"{targetObject.name}_Part_{partIndex + 1}");
            partObject.transform.position = targetObject.transform.position;
            partObject.transform.rotation = targetObject.transform.rotation;
            partObject.transform.localScale = Vector3.one;
            partObject.transform.SetParent(parent);

            // Copy static setting from original object
            partObject.isStatic = targetObject.isStatic;

            // Copy layer
            partObject.layer = originalLayer;

            // Add components
            MeshFilter mf = partObject.AddComponent<MeshFilter>();
            mf.sharedMesh = newMesh;

            MeshRenderer mr = partObject.AddComponent<MeshRenderer>();
            CopyMeshRendererParameters(originalRenderer, mr, materials);

            // Move origin to center of geometry
            Vector3 meshCenter = newMesh.bounds.center;
            partObject.transform.position += partObject.transform.rotation * meshCenter;

            // Recenter the mesh vertices
            Vector3[] vertices = newMesh.vertices;
            for (int i = 0; i < vertices.Length; i++)
            {
                vertices[i] -= meshCenter;
            }
            newMesh.vertices = vertices;
            newMesh.RecalculateBounds();

            // Update the mesh filter with recentered mesh
            mf.sharedMesh = newMesh;

            // Add collider only if enabled and mesh is valid
            if (addMeshColliders && IsValidForCollider(newMesh))
            {
                MeshCollider newCollider = partObject.AddComponent<MeshCollider>();
                newCollider.sharedMesh = newMesh;
                newCollider.convex = makeCollidersConvex;

                // Copy physics material
                if (originalPhysicsMaterial != null)
                {
                    newCollider.sharedMaterial = originalPhysicsMaterial;
                }
            }

            successfulParts++;
        }

        Debug.Log($"MeshSeparator: Created {successfulParts} separate objects");

        // Optionally disable the original object
        targetObject.SetActive(false);
    }

    /// <summary>
    /// Copies MeshRenderer parameters from the original renderer to the new renderer.
    /// This includes materials, rendering settings, shadows, light probes, and reflection probes.
    /// </summary>
    private void CopyMeshRendererParameters(MeshRenderer sourceRenderer, MeshRenderer targetRenderer, Material[] materials)
    {
        // Copy materials
        if (materials != null && materials.Length > 0)
        {
            targetRenderer.sharedMaterials = materials;
        }

        // If source renderer is null, we can only copy materials
        if (sourceRenderer == null)
            return;

        // Copy rendering settings
        targetRenderer.enabled = sourceRenderer.enabled;
        targetRenderer.shadowCastingMode = sourceRenderer.shadowCastingMode;
        targetRenderer.receiveShadows = sourceRenderer.receiveShadows;
        targetRenderer.motionVectorGenerationMode = sourceRenderer.motionVectorGenerationMode;

        // Copy light probe settings
        targetRenderer.lightProbeUsage = sourceRenderer.lightProbeUsage;
        if (sourceRenderer.lightProbeUsage == LightProbeUsage.UseProxyVolume)
        {
            targetRenderer.lightProbeProxyVolumeOverride = sourceRenderer.lightProbeProxyVolumeOverride;
        }

        // Copy reflection probe settings
        targetRenderer.reflectionProbeUsage = sourceRenderer.reflectionProbeUsage;

        // Copy rendering layer mask (URP/HDRP feature)
        targetRenderer.renderingLayerMask = sourceRenderer.renderingLayerMask;

        // Copy sorting settings
        targetRenderer.sortingLayerID = sourceRenderer.sortingLayerID;
        targetRenderer.sortingOrder = sourceRenderer.sortingOrder;

        // Copy probes optimization settings
        targetRenderer.allowOcclusionWhenDynamic = sourceRenderer.allowOcclusionWhenDynamic;
    }

    /// <summary>
    /// Builds a new mesh from a list of triangle indices (triangle numbers, not raw indices!).
    /// Skips degenerate triangles based on degenerateTriangleSqrAreaThreshold.
    /// </summary>
    private Mesh CreateMeshFromTriangles(Mesh originalMesh, List<int> triangleIndices)
    {
        int[] originalTriangles = originalMesh.triangles;
        Vector3[] originalVertices = originalMesh.vertices;
        Vector3[] originalNormals = originalMesh.normals;
        Vector2[] originalUV = originalMesh.uv;
        Vector2[] originalUV2 = originalMesh.uv2;
        Color[] originalColors = originalMesh.colors;

        Dictionary<int, int> vertexMap = new Dictionary<int, int>();
        List<Vector3> newVertices = new List<Vector3>();
        List<Vector3> newNormals = new List<Vector3>();
        List<Vector2> newUV = new List<Vector2>();
        List<Vector2> newUV2 = new List<Vector2>();
        List<Color> newColors = new List<Color>();
        List<int> newTriangles = new List<int>();

        bool checkDegenerate = degenerateTriangleSqrAreaThreshold > 0f;

        foreach (int triIndex in triangleIndices)
        {
            int baseIndex = triIndex * 3;

            int oldV0 = originalTriangles[baseIndex];
            int oldV1 = originalTriangles[baseIndex + 1];
            int oldV2 = originalTriangles[baseIndex + 2];

            if (checkDegenerate)
            {
                Vector3 v0 = originalVertices[oldV0];
                Vector3 v1 = originalVertices[oldV1];
                Vector3 v2 = originalVertices[oldV2];

                Vector3 edge1 = v1 - v0;
                Vector3 edge2 = v2 - v0;
                float sqArea = Vector3.Cross(edge1, edge2).sqrMagnitude;

                if (sqArea < degenerateTriangleSqrAreaThreshold)
                {
                    // Skip this degenerate (or nearly degenerate) triangle
                    continue;
                }
            }

            int newV0 = GetOrCreateVertex(oldV0, originalVertices, originalNormals, originalUV, originalUV2, originalColors,
                                          vertexMap, newVertices, newNormals, newUV, newUV2, newColors);
            int newV1 = GetOrCreateVertex(oldV1, originalVertices, originalNormals, originalUV, originalUV2, originalColors,
                                          vertexMap, newVertices, newNormals, newUV, newUV2, newColors);
            int newV2 = GetOrCreateVertex(oldV2, originalVertices, originalNormals, originalUV, originalUV2, originalColors,
                                          vertexMap, newVertices, newNormals, newUV, newUV2, newColors);

            newTriangles.Add(newV0);
            newTriangles.Add(newV1);
            newTriangles.Add(newV2);
        }

        if (newVertices.Count == 0 || newTriangles.Count == 0)
            return null;

        Mesh newMesh = new Mesh();
        newMesh.name = $"SeparatedPart_{newTriangles.Count / 3}tris";
        newMesh.SetVertices(newVertices);

        if (newNormals.Count > 0)
            newMesh.SetNormals(newNormals);

        if (newUV.Count > 0)
            newMesh.SetUVs(0, newUV);

        if (newUV2.Count > 0)
            newMesh.SetUVs(1, newUV2);

        if (newColors.Count > 0)
            newMesh.SetColors(newColors);

        newMesh.SetTriangles(newTriangles, 0);

        if (newNormals.Count == 0)
            newMesh.RecalculateNormals();

        newMesh.RecalculateBounds();

        return newMesh;
    }

    private int GetOrCreateVertex(
        int oldVertIndex,
        Vector3[] originalVertices,
        Vector3[] originalNormals,
        Vector2[] originalUV,
        Vector2[] originalUV2,
        Color[] originalColors,
        Dictionary<int, int> vertexMap,
        List<Vector3> newVertices,
        List<Vector3> newNormals,
        List<Vector2> newUV,
        List<Vector2> newUV2,
        List<Color> newColors)
    {
        if (vertexMap.TryGetValue(oldVertIndex, out int mapped))
            return mapped;

        int newVertIndex = newVertices.Count;
        vertexMap[oldVertIndex] = newVertIndex;

        newVertices.Add(originalVertices[oldVertIndex]);

        if (originalNormals != null && originalNormals.Length > oldVertIndex)
            newNormals.Add(originalNormals[oldVertIndex]);

        if (originalUV != null && originalUV.Length > oldVertIndex)
            newUV.Add(originalUV[oldVertIndex]);

        if (originalUV2 != null && originalUV2.Length > oldVertIndex)
            newUV2.Add(originalUV2[oldVertIndex]);

        if (originalColors != null && originalColors.Length > oldVertIndex)
            newColors.Add(originalColors[oldVertIndex]);

        return newVertIndex;
    }

    /// <summary>
    /// Validates if a mesh is suitable for a mesh collider.
    /// </summary>
    private bool IsValidForCollider(Mesh mesh)
    {
        if (mesh == null || mesh.vertexCount == 0 || mesh.triangles == null || mesh.triangles.Length == 0)
            return false;

        if (maxColliderVertices > 0 && mesh.vertexCount > maxColliderVertices)
        {
            Debug.LogWarning($"MeshSeparator: Mesh has {mesh.vertexCount} vertices, exceeds limit of {maxColliderVertices}. Skipping collider.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Returns the number of loose parts without creating objects.
    /// </summary>
    public int GetLoosePartCount()
    {
        GameObject target = targetObject != null ? targetObject : gameObject;

        MeshFilter meshFilter = target.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return 0;

        return DetectLooseParts(meshFilter.sharedMesh).Count;
    }
}