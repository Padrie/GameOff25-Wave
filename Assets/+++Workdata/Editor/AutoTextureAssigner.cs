using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// Automatically assigns textures to materials using the "Universal Render Pipeline/Refined Lit" shader
/// by searching for folders with the exact name of the material and assigning textures found within.
/// </summary>
public class AutoTextureAssigner : EditorWindow
{
    private const string TARGET_SHADER_NAME = "Universal Render Pipeline/Refined Lit";
    
    private Vector2 scrollPosition;
    private List<ProcessingResult> results = new List<ProcessingResult>();
    private bool showDetailedLog = true;
    
    private class ProcessingResult
    {
        public string materialName;
        public bool success;
        public List<string> messages = new List<string>();
    }
    
    [MenuItem("Tools/Auto Texture Assigner")]
    public static void ShowWindow()
    {
        GetWindow<AutoTextureAssigner>("Auto Texture Assigner");
    }
    
    private void OnGUI()
    {
        GUILayout.Label("Refined Lit Material Auto Texture Assigner", EditorStyles.boldLabel);
        GUILayout.Space(10);
        
        EditorGUILayout.HelpBox(
            "This tool will:\n" +
            "1. Find all materials using the 'Refined Lit' shader\n" +
            "2. Search for folders with the exact material name\n" +
            "3. Automatically assign textures based on naming patterns\n\n" +
            "Texture naming patterns:\n" +
            "• Albedo/Base/Diffuse/Color → Albedo Map\n" +
            "• Roughness → Roughness Map\n" +
            "• Metallic/Metal → Metallic Map\n" +
            "• Specular/Spec → Specular Map\n" +
            "• Normal/Norm → Normal Map\n" +
            "• Height/Parallax/Displacement → Height Map\n" +
            "• Emission/Emissive/Glow → Emission Map",
            MessageType.Info);
        
        GUILayout.Space(10);
        
        showDetailedLog = EditorGUILayout.Toggle("Show Detailed Log", showDetailedLog);
        
        GUILayout.Space(10);
        
        if (GUILayout.Button("Process All Materials", GUILayout.Height(30)))
        {
            ProcessAllMaterials();
        }
        
        if (GUILayout.Button("Process Selected Materials", GUILayout.Height(30)))
        {
            ProcessSelectedMaterials();
        }
        
        GUILayout.Space(10);
        
        // Display results
        if (results.Count > 0)
        {
            GUILayout.Label("Processing Results:", EditorStyles.boldLabel);
            
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(300));
            
            foreach (var result in results)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                
                GUIStyle style = new GUIStyle(EditorStyles.label);
                style.fontStyle = FontStyle.Bold;
                style.normal.textColor = result.success ? Color.green : Color.yellow;
                
                EditorGUILayout.LabelField(result.materialName, style);
                
                if (showDetailedLog)
                {
                    foreach (var message in result.messages)
                    {
                        EditorGUILayout.LabelField("  • " + message, EditorStyles.wordWrappedLabel);
                    }
                }
                
                EditorGUILayout.EndVertical();
                GUILayout.Space(5);
            }
            
            EditorGUILayout.EndScrollView();
            
            if (GUILayout.Button("Clear Results"))
            {
                results.Clear();
            }
        }
    }
    
    private void ProcessSelectedMaterials()
    {
        results.Clear();
        
        var selectedMaterials = Selection.objects
            .OfType<Material>()
            .Where(m => m.shader != null && m.shader.name == TARGET_SHADER_NAME)
            .ToList();
        
        if (selectedMaterials.Count == 0)
        {
            EditorUtility.DisplayDialog("No Valid Materials Selected", 
                "Please select at least one material using the 'Refined Lit' shader.", "OK");
            return;
        }
        
        ProcessMaterials(selectedMaterials);
    }
    
    private void ProcessAllMaterials()
    {
        results.Clear();
        
        string[] materialGUIDs = AssetDatabase.FindAssets("t:Material");
        List<Material> materials = new List<Material>();
        
        foreach (string guid in materialGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            
            if (mat != null && mat.shader != null && mat.shader.name == TARGET_SHADER_NAME)
            {
                materials.Add(mat);
            }
        }
        
        if (materials.Count == 0)
        {
            EditorUtility.DisplayDialog("No Materials Found", 
                "No materials using the 'Refined Lit' shader were found in the project.", "OK");
            return;
        }
        
        ProcessMaterials(materials);
    }
    
    private void ProcessMaterials(List<Material> materials)
    {
        EditorUtility.DisplayProgressBar("Processing Materials", "Searching for textures...", 0f);
        
        try
        {
            for (int i = 0; i < materials.Count; i++)
            {
                Material mat = materials[i];
                float progress = (float)i / materials.Count;
                EditorUtility.DisplayProgressBar("Processing Materials", 
                    $"Processing {mat.name}... ({i + 1}/{materials.Count})", progress);
                
                ProcessSingleMaterial(mat);
            }
            
            EditorUtility.DisplayDialog("Processing Complete", 
                $"Processed {materials.Count} material(s). Check the results below.", "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
        
        Repaint();
    }
    
    private void ProcessSingleMaterial(Material material)
    {
        ProcessingResult result = new ProcessingResult
        {
            materialName = material.name,
            success = false
        };
        
        // Find folder with exact material name
        string[] folderGUIDs = AssetDatabase.FindAssets($"{material.name} t:folder");
        string materialFolderPath = null;
        
        foreach (string guid in folderGUIDs)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string folderName = Path.GetFileName(path);
            
            // Check for exact name match
            if (folderName == material.name)
            {
                materialFolderPath = path;
                break;
            }
        }
        
        if (string.IsNullOrEmpty(materialFolderPath))
        {
            result.messages.Add($"No folder found with exact name '{material.name}'");
            results.Add(result);
            return;
        }
        
        result.messages.Add($"Found folder: {materialFolderPath}");
        
        // Find all textures in the folder
        string[] textureGUIDs = AssetDatabase.FindAssets("t:texture2D", new[] { materialFolderPath });
        
        if (textureGUIDs.Length == 0)
        {
            result.messages.Add("No textures found in folder");
            results.Add(result);
            return;
        }
        
        bool anyAssigned = false;
        
        foreach (string guid in textureGUIDs)
        {
            string texturePath = AssetDatabase.GUIDToAssetPath(guid);
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            
            if (texture == null) continue;
            
            string textureName = texture.name.ToLower();
            
            // Assign textures based on naming patterns
            if (AssignIfMatch(material, texture, textureName, 
                new[] { "albedo", "base", "diffuse", "color", "basecolor" }, 
                "_BaseMap", "Albedo Map", result))
            {
                anyAssigned = true;
                continue;
            }
            
            if (AssignIfMatch(material, texture, textureName, 
                new[] { "roughness", "rough" }, 
                "_RoughnessMap", "Roughness Map", result))
            {
                anyAssigned = true;
                continue;
            }
            
            if (AssignIfMatch(material, texture, textureName, 
                new[] { "metallic", "metal" }, 
                "_MetallicMap", "Metallic Map", result))
            {
                anyAssigned = true;
                continue;
            }
            
            if (AssignIfMatch(material, texture, textureName, 
                new[] { "specular", "spec" }, 
                "_SpecGlossMap", "Specular Map", result))
            {
                anyAssigned = true;
                continue;
            }
            
            if (AssignIfMatch(material, texture, textureName, 
                new[] { "normal", "norm", "nrm" }, 
                "_BumpMap", "Normal Map", result))
            {
                // Set normal map import settings
                SetTextureAsNormalMap(texturePath);
                anyAssigned = true;
                continue;
            }
            
            if (AssignIfMatch(material, texture, textureName, 
                new[] { "height", "parallax", "displacement", "disp" }, 
                "_ParallaxMap", "Height Map", result))
            {
                anyAssigned = true;
                continue;
            }
            
            if (AssignIfMatch(material, texture, textureName, 
                new[] { "emission", "emissive", "glow" }, 
                "_EmissionMap", "Emission Map", result))
            {
                anyAssigned = true;
                continue;
            }
        }
        
        if (anyAssigned)
        {
            result.success = true;
            EditorUtility.SetDirty(material);
        }
        else
        {
            result.messages.Add("No textures matched naming patterns");
        }
        
        results.Add(result);
    }
    
    private bool AssignIfMatch(Material material, Texture2D texture, string textureName, 
        string[] keywords, string propertyName, string displayName, ProcessingResult result)
    {
        foreach (string keyword in keywords)
        {
            if (textureName.Contains(keyword))
            {
                if (material.HasProperty(propertyName))
                {
                    material.SetTexture(propertyName, texture);
                    result.messages.Add($"✓ Assigned {displayName}: {texture.name}");
                    return true;
                }
            }
        }
        return false;
    }
    
    private void SetTextureAsNormalMap(string texturePath)
    {
        TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.NormalMap)
        {
            importer.textureType = TextureImporterType.NormalMap;
            importer.SaveAndReimport();
        }
    }
}
