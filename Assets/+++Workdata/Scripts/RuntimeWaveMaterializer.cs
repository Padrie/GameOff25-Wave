using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class RuntimeWaveMaterializer : MonoBehaviour
{
    public Material waveTemplate;
    public Renderer[] targets;
    public bool includeChildren = true;

    static readonly int BaseMapID = Shader.PropertyToID("_BaseMap");
    static readonly int MainTexID = Shader.PropertyToID("_MainTex");
    static readonly int BaseColorID = Shader.PropertyToID("_BaseColor");

    readonly Dictionary<int, Material> cache = new Dictionary<int, Material>();
    MaterialPropertyBlock mpb;

    void Awake() { if (mpb == null) mpb = new MaterialPropertyBlock(); }
    void OnEnable() { if (mpb == null) mpb = new MaterialPropertyBlock(); Apply(); }
#if UNITY_EDITOR
    void OnValidate() { if (mpb == null) mpb = new MaterialPropertyBlock(); Apply(); }
#endif

    static string TrimWaveTag(string n)
    {
        while (n.StartsWith("Wave<") && n.EndsWith(">"))
            n = n.Substring(5, n.Length - 6);
        return n;
    }

    bool IsWaveMat(Material m) => m && waveTemplate && m.shader == waveTemplate.shader;

    public void Apply()
    {
        if (!waveTemplate) return;

        var list = new List<Renderer>();
        if (targets != null && targets.Length > 0) list.AddRange(targets);
        if (includeChildren) list.AddRange(GetComponentsInChildren<Renderer>(true));

        foreach (var rend in list)
        {
            if (!rend) continue;

            var srcMats = rend.sharedMaterials;
            if (srcMats == null || srcMats.Length == 0) continue;

            var dstMats = new Material[srcMats.Length];

            for (int i = 0; i < srcMats.Length; i++)
            {
                var src = srcMats[i];
                if (!src) { dstMats[i] = waveTemplate; continue; }

                if (IsWaveMat(src)) { dstMats[i] = src; continue; }

                var key = src.GetInstanceID();
                if (!cache.TryGetValue(key, out var dst) || !dst)
                {
                    dst = new Material(waveTemplate)
                    {
                        renderQueue = src.renderQueue,
                        name = $"Wave<{TrimWaveTag(src.name)}>"
                    };

                    Texture tex = null;
                    Vector2 scale = Vector2.one, offset = Vector2.zero;

                    if (src.HasProperty(BaseMapID))
                    {
                        tex = src.GetTexture(BaseMapID);
                        scale = src.GetTextureScale(BaseMapID);
                        offset = src.GetTextureOffset(BaseMapID);
                    }
                    else if (src.HasProperty(MainTexID))
                    {
                        tex = src.GetTexture(MainTexID);
                        scale = src.GetTextureScale(MainTexID);
                        offset = src.GetTextureOffset(MainTexID);
                    }

                    if (tex)
                    {
                        dst.SetTexture(BaseMapID, tex);
                        dst.SetTextureScale(BaseMapID, scale);
                        dst.SetTextureOffset(BaseMapID, offset);
                    }

                    if (src.HasProperty(BaseColorID))
                        dst.SetColor(BaseColorID, src.GetColor(BaseColorID));

                    cache[key] = dst;
                }

                dstMats[i] = dst;
            }

            rend.enabled = true;
            mpb.Clear();
            rend.SetPropertyBlock(mpb);
            rend.sharedMaterials = dstMats;
        }
    }
}
