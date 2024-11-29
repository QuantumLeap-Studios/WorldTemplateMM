using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.IO.Compression;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using System.Collections;
using Unity.Plastic.Newtonsoft.Json;
using System.Collections.Generic;
using System;

public class BuildAssetBundleMMWorld : EditorWindow
{
    private string assetBundleDirectory = "Assets/World";
    private string outputDirectory = "Assets/World/Output";
    private string zipName = "WorldAssetBundle";

    private const string AssetBundleDirectoryKey = "AssetBundleDirectory";
    private const string OutputDirectoryKey = "OutputDirectory";
    private const string ZipNameKey = "ZipName";

    private BuildTarget[] supportedTargets = new BuildTarget[]
    {
        BuildTarget.StandaloneWindows,
        BuildTarget.Android
    };

    [MenuItem("Tools/Build .mmworld Asset Bundle")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(BuildAssetBundleMMWorld));
    }

    void OnEnable()
    {
        assetBundleDirectory = EditorPrefs.GetString(AssetBundleDirectoryKey, "Assets/World");
        outputDirectory = EditorPrefs.GetString(OutputDirectoryKey, "Assets/World/Output");
        zipName = EditorPrefs.GetString(ZipNameKey, "WorldAssetBundle");
    }

    void OnGUI()
    {
        GUILayout.Label("Build .mmworld Asset Bundle", EditorStyles.boldLabel);

        assetBundleDirectory = EditorGUILayout.TextField("Asset Bundle Directory", assetBundleDirectory);
        outputDirectory = EditorGUILayout.TextField("Output Directory", outputDirectory);
        zipName = EditorGUILayout.TextField("Zip Name", zipName);

        if (GUILayout.Button("Build Asset Bundles for All Platforms"))
        {
            BuildAssetBundlesForAllPlatforms();
        }

        if (GUILayout.Button("Save Preferences"))
        {
            SavePreferences();
        }
    }

    void SavePreferences()
    {
        EditorPrefs.SetString(AssetBundleDirectoryKey, assetBundleDirectory);
        EditorPrefs.SetString(OutputDirectoryKey, outputDirectory);
        EditorPrefs.SetString(ZipNameKey, zipName);

        Debug.Log("Preferences saved.");
    }

    void BuildAssetBundlesForAllPlatforms()
    {
        if (!Directory.Exists(assetBundleDirectory))
        {
            Debug.LogError("Asset bundle directory does not exist!");
            return;
        }

        if (string.IsNullOrEmpty(outputDirectory))
        {
            outputDirectory = EditorUtility.SaveFolderPanel("Select Output Directory", "Assets", "");
            if (string.IsNullOrEmpty(outputDirectory)) return;
        }

        if (!Directory.Exists(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        string[] scenePaths = Directory.GetFiles(assetBundleDirectory, "*.unity", SearchOption.AllDirectories);

        if (scenePaths.Length == 0)
        {
            Debug.LogError("No scene files found in the directory to build!");
            return;
        }

        Debug.Log("Starting Asset Bundle build...");

        try
        {
            BuildAssetBundles();

            string zipFilePath = Path.Combine(outputDirectory, $"{zipName}.zip");

            if (File.Exists(zipFilePath))
            {
                File.Delete(zipFilePath);
            }

            using (var zip = ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
            {
                foreach (BuildTarget target in supportedTargets)
                {
                    string bundlePath = Path.Combine(outputDirectory, $"{zipName}{target}.bundle");

                    if (File.Exists(bundlePath))
                    {
                        zip.CreateEntryFromFile(bundlePath, Path.GetFileName(bundlePath), CompressionLevel.Optimal);
                        File.Delete(bundlePath);
                        Debug.Log($"Added {Path.GetFileName(bundlePath)} to the zip.");
                    }
                    else
                    {
                        Debug.LogError($"Asset bundle not found for {target} at {bundlePath}");
                    }
                }
            }

            Debug.Log($"Asset bundles zipped to: {zipFilePath}");
            AssetDatabase.Refresh();
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Build failed: {ex.Message}");
        }
    }

    void BuildAssetBundles()
    {
        foreach (BuildTarget target in supportedTargets)
        {
            AssetBundleBuild[] buildMap = new AssetBundleBuild[1];
            buildMap[0].assetBundleName = $"{zipName}{target}.bundle";
            buildMap[0].assetNames = Directory.GetFiles(assetBundleDirectory, "*.unity", SearchOption.AllDirectories);

            Debug.Log($"Building asset bundle for {target} with {buildMap[0].assetNames.Length} scene(s)");

            BuildPipeline.BuildAssetBundles(outputDirectory, buildMap, BuildAssetBundleOptions.UncompressedAssetBundle, target);
        }
    }

    void CreateZipFile(string bundlePath)
    {
        string zipFilePath = Path.Combine(outputDirectory, $"{zipName}.zip");

        bool zipExists = File.Exists(zipFilePath);

        using (var zip = zipExists ? ZipFile.Open(zipFilePath, ZipArchiveMode.Update) : ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
        {
            zip.CreateEntryFromFile(bundlePath, Path.GetFileName(bundlePath), CompressionLevel.Optimal);

            Debug.Log($"Added {Path.GetFileName(bundlePath)} to the zip.");
        }

        File.Delete(bundlePath);

        Debug.Log($"Asset bundle zipped to: {zipFilePath}");
    }
}
