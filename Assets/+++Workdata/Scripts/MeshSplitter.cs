using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Splits a mesh into multiple parts based on a target surface area (in m²).
/// Uses a grid in X/Z to create roughly square chunks, then merges very small
/// pieces into the nearest larger one.
/// </summary>
[ExecuteInEditMode]
public class MeshSplitter : MonoBehaviour
{
    [Header("Target Object")]
    [Tooltip("The GameObject whose mesh will be cut. If null, this GameObject is used.")]
    public GameObject targetObject;

    [Header("Cut Settings")]
    [Tooltip("Target surface area in square meters for each part (approximate).")]
    public float targetArea = 1.0f;

    [Tooltip("Create separate GameObjects for each part.")]
    public bool createSeparateObjects = true;

    [Header("Output Parent")]
    [Tooltip("Optional parent under which all created parts will be placed. " +
             "If null, parts are created as siblings of the original object.")]
    public GameObject outputParent;

    [Header("Original Object Handling")]
    [Tooltip("Disable the original GameObject after cutting.")]
    public bool disableOriginal = false;

    [Tooltip("Delete the original MeshFilter/MeshRenderer after cutting.")]
    public bool removeOriginalMeshComponents = false;

    [Header("Collider Settings")]
    [Tooltip("Add MeshCollider to each part (in addition to copying if original has one).")]
    public bool addMeshCollider = false;

    [Tooltip("If original has no MeshCollider, new ones will use this convex setting.")]
    public bool makeCollidersConvex = true;

    [Header("Gizmo Settings")]
    [Tooltip("Show gizmos for debugging parts.")]
    public bool showGizmos = false;

    private List<List<int>> cachedParts;
    private Mesh cachedMesh;

    private void OnValidate()
    {
        if (targetObject == null)
            targetObject = gameObject;

        if (targetArea <= 0f)
            targetArea = 0.1f;
    }

    [ContextMenu("Cut By Area")]
    public void CutByAreaContextMenu()
    {
        CutByArea();
    }

    public void CutByArea()
    {
        GameObject target = targetObject != null ? targetObject : gameObject;

        MeshFilter meshFilter = target.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
        {
            Debug.LogWarning("MeshAreaCutter: No MeshFilter or mesh found on target object!");
            return;
        }

        // Remove previously generated parts so we replace them
        RemovePreviousParts(target);

        Mesh originalMesh = meshFilter.sharedMesh;
        List<List<int>> parts = DetectAreaParts(originalMesh);

        Debug.Log($"MeshAreaCutter: Created {parts.Count} part(s) with target area {targetArea} m²");

        if (createSeparateObjects && parts.Count > 1)
        {
            CreateSeparateGameObjects(target, originalMesh, parts);
        }
    }

    /// <summary>
    /// Splits the mesh into triangle groups based on a grid in X/Z.
    /// Each cell gets triangles whose centroids fall into it.
    /// Very small cells are merged into the nearest bigger cell.
    /// Returns triangle index lists (triIndex, not vertex index).
    /// </summary>
    private List<List<int>> DetectAreaParts(Mesh mesh)
    {
        List<List<int>> parts = new List<List<int>>();

        int[] triangles = mesh.triangles;
        Vector3[] vertices = mesh.vertices;

        int triangleCount = triangles.Length / 3;
        if (triangleCount == 0)
            return parts;

        float[] triAreas = new float[triangleCount];
        Vector3[] triCentroids = new Vector3[triangleCount];

        float totalArea = 0f;
        float minX = float.PositiveInfinity;
        float maxX = float.NegativeInfinity;
        float minZ = float.PositiveInfinity;
        float maxZ = float.NegativeInfinity;

        // Precompute area & centroid, and bounds in XZ
        for (int triIndex = 0; triIndex < triangleCount; triIndex++)
        {
            int baseIndex = triIndex * 3;
            int v0 = triangles[baseIndex];
            int v1 = triangles[baseIndex + 1];
            int v2 = triangles[baseIndex + 2];

            Vector3 p0 = vertices[v0];
            Vector3 p1 = vertices[v1];
            Vector3 p2 = vertices[v2];

            float area = ComputeTriangleArea(p0, p1, p2);
            triAreas[triIndex] = area;
            totalArea += area;

            Vector3 centroid = (p0 + p1 + p2) / 3f;
            triCentroids[triIndex] = centroid;

            if (centroid.x < minX) minX = centroid.x;
            if (centroid.x > maxX) maxX = centroid.x;
            if (centroid.z < minZ) minZ = centroid.z;
            if (centroid.z > maxZ) maxZ = centroid.z;
        }

        if (totalArea <= Mathf.Epsilon || targetArea <= Mathf.Epsilon)
        {
            List<int> all = new List<int>();
            for (int i = 0; i < triangleCount; i++)
                all.Add(i);
            parts.Add(all);
            return parts;
        }

        // Rough desired number of parts
        int desiredPartCount = Mathf.Max(1, Mathf.RoundToInt(totalArea / targetArea));

        // Build a roughly square grid in XZ
        int cols = Mathf.CeilToInt(Mathf.Sqrt(desiredPartCount));
        int rows = Mathf.CeilToInt((float)desiredPartCount / cols);

        float sizeX = (maxX - minX) / Mathf.Max(cols, 1);
        float sizeZ = (maxZ - minZ) / Mathf.Max(rows, 1);

        if (sizeX <= Mathf.Epsilon || sizeZ <= Mathf.Epsilon)
        {
            List<int> all = new List<int>();
            for (int i = 0; i < triangleCount; i++)
                all.Add(i);
            parts.Add(all);
            return parts;
        }

        int gridPartCount = rows * cols;
        List<int>[] gridParts = new List<int>[gridPartCount];
        float[] gridAreas = new float[gridPartCount];

        // Assign each triangle to a grid cell by centroid
        for (int triIndex = 0; triIndex < triangleCount; triIndex++)
        {
            Vector3 c = triCentroids[triIndex];
            int col = Mathf.Clamp((int)((c.x - minX) / sizeX), 0, cols - 1);
            int row = Mathf.Clamp((int)((c.z - minZ) / sizeZ), 0, rows - 1);

            int partId = row * cols + col;
            if (gridParts[partId] == null)
                gridParts[partId] = new List<int>();

            gridParts[partId].Add(triIndex);
            gridAreas[partId] += triAreas[triIndex];
        }

        // Compute centers for non-empty parts
        Vector3[] gridCenters = new Vector3[gridPartCount];
        bool[] hasTriangles = new bool[gridPartCount];

        for (int pid = 0; pid < gridPartCount; pid++)
        {
            List<int> tris = gridParts[pid];
            if (tris == null || tris.Count == 0)
                continue;

            hasTriangles[pid] = true;
            Vector3 sum = Vector3.zero;
            foreach (int triIndex in tris)
                sum += triCentroids[triIndex];

            gridCenters[pid] = sum / tris.Count;
        }

        // Merge very small parts into nearest larger one
        float minSmallArea = targetArea * 0.25f;

        List<int> largeIds = new List<int>();
        List<int> smallIds = new List<int>();

        for (int pid = 0; pid < gridPartCount; pid++)
        {
            if (!hasTriangles[pid])
                continue;

            if (gridAreas[pid] < minSmallArea)
                smallIds.Add(pid);
            else
                largeIds.Add(pid);
        }

        if (largeIds.Count == 0)
        {
            for (int i = 0; i < smallIds.Count; i++)
                largeIds.Add(smallIds[i]);
            smallIds.Clear();
        }

        foreach (int smallId in smallIds)
        {
            List<int> smallTris = gridParts[smallId];
            if (smallTris == null || smallTris.Count == 0)
                continue;

            Vector3 centerSmall = gridCenters[smallId];
            float bestDistSq = float.MaxValue;
            int bestLargeId = largeIds[0];

            foreach (int largeId in largeIds)
            {
                Vector3 centerLarge = gridCenters[largeId];
                Vector2 a = new Vector2(centerSmall.x, centerSmall.z);
                Vector2 b = new Vector2(centerLarge.x, centerLarge.z);
                float distSq = (a - b).sqrMagnitude;
                if (distSq < bestDistSq)
                {
                    bestDistSq = distSq;
                    bestLargeId = largeId;
                }
            }

            if (gridParts[bestLargeId] == null)
                gridParts[bestLargeId] = new List<int>();

            gridParts[bestLargeId].AddRange(smallTris);
            gridAreas[bestLargeId] += gridAreas[smallId];

            gridParts[smallId] = null;
            hasTriangles[smallId] = false;
        }

        for (int pid = 0; pid < gridPartCount; pid++)
        {
            List<int> tris = gridParts[pid];
            if (tris != null && tris.Count > 0)
                parts.Add(tris);
        }

        return parts;
    }

    private float ComputeTriangleArea(Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 ab = b - a;
        Vector3 ac = c - a;
        Vector3 cross = Vector3.Cross(ab, ac);
        return 0.5f * cross.magnitude;
    }

    private long GetEdgeKey(int v0, int v1)
    {
        if (v0 > v1) (v0, v1) = (v1, v0);
        return ((long)v0 << 32) | (uint)v1;
    }

    /// <summary>
    /// Removes previously generated parts so a new cut replaces them.
    /// Only destroys children of the chosen parent whose name matches OriginalName_AreaPart_X.
    /// </summary>
    private void RemovePreviousParts(GameObject originalObject)
    {
        Transform parent;

        if (outputParent != null)
        {
            parent = outputParent.transform;
        }
        else
        {
            parent = originalObject.transform.parent != null
                ? originalObject.transform.parent
                : originalObject.transform;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);
            if (child == originalObject.transform)
                continue;

            if (child.name.StartsWith(originalObject.name + "_AreaPart_"))
            {
                if (Application.isEditor && !Application.isPlaying)
                    DestroyImmediate(child.gameObject);
                else
                    Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// Creates separate GameObjects for each part.
    /// Children of outputParent if set, otherwise siblings of original.
    /// Copies MeshRenderer, MeshCollider, layer, static flag from original.
    /// </summary>
    private void CreateSeparateGameObjects(GameObject originalObject, Mesh originalMesh, List<List<int>> parts)
    {
        Transform parentTransform;

        if (outputParent != null)
            parentTransform = outputParent.transform;
        else
            parentTransform = originalObject.transform.parent;

        MeshRenderer origMr = originalObject.GetComponent<MeshRenderer>();
        MeshCollider origMc = originalObject.GetComponent<MeshCollider>();

        for (int partIndex = 0; partIndex < parts.Count; partIndex++)
        {
            List<int> partTriangles = parts[partIndex];
            Mesh partMesh = CreateMeshFromTriangles(originalMesh, partTriangles);

            GameObject partObj = new GameObject(originalObject.name + "_AreaPart_" + partIndex);
            partObj.transform.SetParent(parentTransform, false);
            partObj.transform.position = originalObject.transform.position;
            partObj.transform.rotation = originalObject.transform.rotation;
            partObj.transform.localScale = originalObject.transform.localScale;

            partObj.layer = originalObject.layer;
            partObj.isStatic = originalObject.isStatic;

            MeshFilter mf = partObj.AddComponent<MeshFilter>();
            mf.sharedMesh = partMesh;

            MeshRenderer newMr = partObj.AddComponent<MeshRenderer>();
            if (origMr != null)
            {
                // Always enable the new renderer
                newMr.enabled = true;

                newMr.sharedMaterials = origMr.sharedMaterials;
                newMr.shadowCastingMode = origMr.shadowCastingMode;
                newMr.receiveShadows = origMr.receiveShadows;
                newMr.motionVectorGenerationMode = origMr.motionVectorGenerationMode;
                newMr.lightProbeUsage = origMr.lightProbeUsage;
                newMr.reflectionProbeUsage = origMr.reflectionProbeUsage;
                newMr.probeAnchor = origMr.probeAnchor;
                newMr.allowOcclusionWhenDynamic = origMr.allowOcclusionWhenDynamic;
                newMr.sortingLayerID = origMr.sortingLayerID;
                newMr.sortingOrder = origMr.sortingOrder;
            }
            else
            {
                newMr.enabled = true;
            }

            if (addMeshCollider || origMc != null)
            {
                MeshCollider mc = partObj.AddComponent<MeshCollider>();
                mc.sharedMesh = partMesh;

                if (origMc != null)
                {
                    mc.convex = origMc.convex;
                    mc.isTrigger = origMc.isTrigger;
                    mc.sharedMaterial = origMc.sharedMaterial;
#if UNITY_2019_1_OR_NEWER
                    mc.cookingOptions = origMc.cookingOptions;
#endif
                }
                else
                {
                    mc.convex = makeCollidersConvex;
                }
            }
        }

        if (disableOriginal)
        {
            originalObject.SetActive(false);
        }

        if (removeOriginalMeshComponents)
        {
            MeshFilter origMf = originalObject.GetComponent<MeshFilter>();
            MeshRenderer origMr2 = originalObject.GetComponent<MeshRenderer>();

            if (origMf != null)
            {
                if (Application.isEditor && !Application.isPlaying)
                    DestroyImmediate(origMf);
                else
                    Destroy(origMf);
            }

            if (origMr2 != null)
            {
                if (Application.isEditor && !Application.isPlaying)
                    DestroyImmediate(origMr2);
                else
                    Destroy(origMr2);
            }
        }
    }

    private Mesh CreateMeshFromTriangles(Mesh originalMesh, List<int> triangleIndices)
    {
        Mesh newMesh = new Mesh();
        newMesh.indexFormat = originalMesh.indexFormat;

        Vector3[] originalVertices = originalMesh.vertices;
        Vector3[] originalNormals = originalMesh.normals;
        Vector2[] originalUV = originalMesh.uv;
        Color[] originalColors = originalMesh.colors;
        int[] originalTriangles = originalMesh.triangles;

        Dictionary<int, int> oldToNewVertexMap = new Dictionary<int, int>();
        List<Vector3> newVertices = new List<Vector3>();
        List<Vector3> newNormals = new List<Vector3>();
        List<Vector2> newUVs = new List<Vector2>();
        List<Color> newColors = new List<Color>();
        List<int> newTriangles = new List<int>();

        foreach (int triIndex in triangleIndices)
        {
            int baseIndex = triIndex * 3;

            for (int i = 0; i < 3; i++)
            {
                int oldVertIndex = originalTriangles[baseIndex + i];

                if (!oldToNewVertexMap.TryGetValue(oldVertIndex, out int newVertIndex))
                {
                    newVertIndex = newVertices.Count;
                    oldToNewVertexMap[oldVertIndex] = newVertIndex;

                    newVertices.Add(originalVertices[oldVertIndex]);
                    if (originalNormals != null && originalNormals.Length > 0)
                        newNormals.Add(originalNormals[oldVertIndex]);
                    if (originalUV != null && originalUV.Length > 0)
                        newUVs.Add(originalUV[oldVertIndex]);
                    if (originalColors != null && originalColors.Length > 0)
                        newColors.Add(originalColors[oldVertIndex]);
                }

                newTriangles.Add(newVertIndex);
            }
        }

        newMesh.SetVertices(newVertices);
        newMesh.SetTriangles(newTriangles, 0);

        if (newNormals.Count == newVertices.Count)
            newMesh.SetNormals(newNormals);
        else
            newMesh.RecalculateNormals();

        if (newUVs.Count == newVertices.Count)
            newMesh.SetUVs(0, newUVs);

        if (newColors.Count == newVertices.Count)
            newMesh.SetColors(newColors);

        newMesh.RecalculateBounds();
        return newMesh;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (!showGizmos)
            return;

        GameObject target = targetObject != null ? targetObject : gameObject;
        MeshFilter meshFilter = target.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return;

        Mesh mesh = meshFilter.sharedMesh;

        if (cachedMesh != mesh || cachedParts == null)
        {
            cachedMesh = mesh;
            cachedParts = DetectAreaParts(mesh);
        }

        if (cachedParts == null)
            return;

        Vector3[] vertices = mesh.vertices;
        int[] triangles = mesh.triangles;

        for (int partIndex = 0; partIndex < cachedParts.Count; partIndex++)
        {
            List<int> partTriangles = cachedParts[partIndex];

            Gizmos.color = (partIndex % 2 == 0) ? Color.white : Color.black;

            foreach (int triIndex in partTriangles)
            {
                int baseIndex = triIndex * 3;

                Vector3 p0 = target.transform.TransformPoint(vertices[triangles[baseIndex]]);
                Vector3 p1 = target.transform.TransformPoint(vertices[triangles[baseIndex + 1]]);
                Vector3 p2 = target.transform.TransformPoint(vertices[triangles[baseIndex + 2]]);

                Gizmos.DrawLine(p0, p1);
                Gizmos.DrawLine(p1, p2);
                Gizmos.DrawLine(p2, p0);
            }
        }
    }
#endif

    public int GetPartCount()
    {
        GameObject target = targetObject != null ? targetObject : gameObject;

        MeshFilter meshFilter = target.GetComponent<MeshFilter>();
        if (meshFilter == null || meshFilter.sharedMesh == null)
            return 0;

        return DetectAreaParts(meshFilter.sharedMesh).Count;
    }
}
