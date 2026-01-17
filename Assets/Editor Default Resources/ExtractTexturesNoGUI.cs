using UnityEngine;
using UnityEditor;
using System.IO;

public class ExtractTexturesNoGUI
{
    [MenuItem("Tools/Extract Textures From Selected")]
    static void ExtractTexturesDirectly()
    {
        if (Selection.activeObject == null)
        {
            Debug.LogError("Select a model file first!");
            return;
        }
        
        string assetPath = AssetDatabase.GetAssetPath(Selection.activeObject);
        Object[] allAssets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        
        string modelFolder = Path.GetDirectoryName(assetPath);
        string textureFolder = Path.Combine(modelFolder, "Textures");
        
        if (!Directory.Exists(textureFolder))
            Directory.CreateDirectory(textureFolder);
        
        int extracted = 0;
        
        foreach (Object obj in allAssets)
        {
            if (obj is Texture2D)
            {
                Texture2D texture = obj as Texture2D;
                
                try
                {
                    Texture2D newTexture = Object.Instantiate(texture);
                    string texturePath = Path.Combine(textureFolder, texture.name + ".asset");
                    AssetDatabase.CreateAsset(newTexture, texturePath);
                    
                    extracted++;
                    Debug.Log("Extracted: " + texture.name);
                }
                catch (System.Exception e)
                {
                    Debug.LogError("Failed to extract " + texture.name + ": " + e.Message);
                }
            }
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        
        Debug.Log("Extracted " + extracted + " textures to: " + textureFolder);
    }
}