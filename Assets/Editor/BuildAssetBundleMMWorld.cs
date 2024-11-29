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
    private string tempBundlePath;

    // Preferences keys for saving directories and zip name
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
        // Load saved preferences
        assetBundleDirectory = EditorPrefs.GetString(AssetBundleDirectoryKey, "Assets/World");
        outputDirectory = EditorPrefs.GetString(OutputDirectoryKey, "Assets/World/Output");
        zipName = EditorPrefs.GetString(ZipNameKey, "WorldAssetBundle");
    }

    void OnGUI()
    {
        GUILayout.Label("Build .mmworld Asset Bundle", EditorStyles.boldLabel);

        // Directory and Zip name fields
        assetBundleDirectory = EditorGUILayout.TextField("Asset Bundle Directory", assetBundleDirectory);
        outputDirectory = EditorGUILayout.TextField("Output Directory", outputDirectory);
        zipName = EditorGUILayout.TextField("Zip Name", zipName);

        // Build button
        if (GUILayout.Button("Build Asset Bundles for All Platforms"))
        {
            BuildAssetBundlesForAllPlatforms();
        }

        // Save preferences button
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

        string[] scenesToBuild = scenePaths.Select(path => path.Replace("/", "//")).ToArray();

        Debug.Log("Starting Asset Bundle build...");

        Dictionary<string, string> platformFileNames = new Dictionary<string, string>();

        try
        {
            BuildAssetBundles();

            foreach (BuildTarget target in supportedTargets)
            {
                string tempBundlePath = Path.Combine(outputDirectory, $"{zipName}{target}.mmworld");

                if (!File.Exists(tempBundlePath))
                {
                    Debug.LogError($"Asset bundle not found for {target} at {tempBundlePath}. Please ensure the build was successful.");
                    continue;
                }

                string finalPath = Path.ChangeExtension(tempBundlePath, $".mmworld{target}");

                if (File.Exists(finalPath))
                {
                    File.Delete(finalPath);  // We need to delete the old .mmworld file to rename the new one  
                }

                File.Move(tempBundlePath, finalPath);

                platformFileNames[target.ToString()] = Path.GetFileNameWithoutExtension(finalPath);

                AssetDatabase.Refresh();

                Debug.Log($"Asset Bundle for {target} built and renamed to .mmworld{target} at: {finalPath}");
            }

            // Create package.json before creating the zip file  
            string packageJsonPath = Path.Combine(outputDirectory, "package.json");
            string jsonContent = JsonConvert.SerializeObject(new
            {
                pcFileName = platformFileNames.ContainsKey("StandaloneWindows") ? platformFileNames["StandaloneWindows"] : "defaultPCBundle",
                androidFileName = platformFileNames.ContainsKey("Android") ? platformFileNames["Android"] : "defaultAndroidBundle"
            }, Formatting.Indented);

            File.WriteAllText(packageJsonPath, jsonContent);
            Debug.Log($"package.json created at: {packageJsonPath}");

            // Now create the zip file  
            foreach (BuildTarget target in supportedTargets)
            {
                string finalPath = Path.Combine(outputDirectory, $"{zipName}{target}.mmworld{target}");
                CreateZipFile(finalPath, platformFileNames, target);
            }
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
            buildMap[0].assetBundleName = $"{zipName}{target}.mmworld{target}";
            buildMap[0].assetNames = Directory.GetFiles(assetBundleDirectory, "*.unity", SearchOption.AllDirectories);

            Debug.Log($"Building asset bundle for {target} with {buildMap[0].assetNames.Length} scene(s)");

            BuildPipeline.BuildAssetBundles(outputDirectory, buildMap, BuildAssetBundleOptions.UncompressedAssetBundle, target);
        }
    }

    void CreateZipFile(string bundlePath, Dictionary<string, string> platformFileNames, BuildTarget target)
    {
        string zipFilePath = Path.Combine(outputDirectory, $"{zipName}.zip");

        // Check if the zip file already exists
        bool zipExists = File.Exists(zipFilePath);

        using (var zip = zipExists ? ZipFile.Open(zipFilePath, ZipArchiveMode.Update) : ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
        {
            // Add the bundle file to the zip
            zip.CreateEntryFromFile(bundlePath, Path.GetFileName(bundlePath), CompressionLevel.Optimal);

            // Add all other platform bundles to the zip
            string[] platformBundles = Directory.GetFiles(outputDirectory, $"*.mmworld{target}");
            foreach (var platformBundle in platformBundles)
            {
                zip.CreateEntryFromFile(platformBundle, Path.GetFileName(platformBundle), CompressionLevel.Optimal);
            }

            // Add package.json file to the zip if it exists
            string packageJsonPath = Path.Combine(outputDirectory, "package.json");
            if (File.Exists(packageJsonPath))
            {
                zip.CreateEntryFromFile(packageJsonPath, "package.json", CompressionLevel.Optimal);
                Debug.Log("Added package.json to the zip.");
            }
            else
            {
                Debug.LogWarning("package.json does not exist. It was not added to the zip.");
            }
        }

        // Optionally, delete the bundle after adding it to the zip
        File.Delete(bundlePath);

        Debug.Log($"Asset bundle and package.json zipped to: {zipFilePath}");
    }
}
