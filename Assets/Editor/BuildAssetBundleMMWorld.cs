using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.IO.Compression;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine.SceneManagement;

public class BuildAssetBundleMMWorld : EditorWindow
{
    private string assetBundleDirectory = "Assets/World";
    private string outputDirectory = "Assets/World/Output";
    private string zipName = "WorldAssetBundle";
    private bool usePostProccessing = true;
    private GameObject globalVolume;

    private const string AssetBundleDirectoryKey = "AssetBundleDirectory";
    private const string OutputDirectoryKey = "OutputDirectory";
    private const string ZipNameKey = "ZipName";
    private const string usePostProccessingKey = "UsePostProccessing";

    private BuildTarget[] supportedTargets = new BuildTarget[]
    {
        BuildTarget.StandaloneWindows,
        BuildTarget.Android
    };

    [MenuItem("Tools/Build .mmworld Asset Bundle")]
    public static void ShowWindow()
    {
        EditorWindow.GetWindow(typeof(BuildAssetBundleMMWorld), false, ".mmworld Builder");
    }

    void OnEnable()
    {
        globalVolume = GameObject.Find("DisableOnBuild").transform.GetChild(0).gameObject;
        assetBundleDirectory = EditorPrefs.GetString(AssetBundleDirectoryKey, "Assets/World");
        outputDirectory = EditorPrefs.GetString(OutputDirectoryKey, "Assets/World/Output");
        zipName = EditorPrefs.GetString(ZipNameKey, "WorldAssetBundle");
        usePostProccessing = EditorPrefs.GetBool(usePostProccessingKey, true);
    }

    void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("Mango Monkeys World Exporter", new GUIStyle(EditorStyles.boldLabel) { fontSize = 16, alignment = TextAnchor.MiddleCenter });
        GUILayout.Space(10);

        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Paths", EditorStyles.boldLabel);
        assetBundleDirectory = EditorGUILayout.TextField("Asset Bundle Directory", assetBundleDirectory);
        outputDirectory = EditorGUILayout.TextField("Output Directory", outputDirectory);
        zipName = EditorGUILayout.TextField("Zip Name", zipName);
        usePostProccessing = EditorGUILayout.Toggle("Use Post Proccessing", usePostProccessing);
        EditorGUILayout.EndVertical();

        GUILayout.Space(10);

        if (zipName.Length > 0 && assetBundleDirectory.Length > 0 && outputDirectory.Length > 0)
        {
            if (GUILayout.Button("Build Asset Bundles for All Platforms", GUILayout.Height(40)))
            {
                BuildAssetBundlesForAllPlatforms();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("One of your fields is empty!", MessageType.Error);
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Save Preferences", GUILayout.Height(30)))
        {
            SavePreferences();
        }

        GUILayout.Space(10);

        EditorGUILayout.HelpBox("Ensure all paths and names are correct before building.", MessageType.Info);
        EditorGUILayout.HelpBox("The output will include a zip package, with it you can export it to mod.io!", MessageType.Info);
        EditorGUILayout.HelpBox("Press Ctrl + R if the zip file isnt showing in Worlds/Output.", MessageType.Info);
        EditorGUILayout.HelpBox("Videos will NOT work on standalone quest.", MessageType.Warning);

        if (globalVolume != null)
        {
            bool shouldBeActive = usePostProccessing;
            if (globalVolume.activeSelf != shouldBeActive)
            {
                globalVolume.SetActive(shouldBeActive);
                EditorUtility.SetDirty(globalVolume);
            }
        }
    }

    private void OnValidate()
    {
        if (globalVolume != null)
        {
            bool shouldBeActive = usePostProccessing;
            if (globalVolume.activeSelf != shouldBeActive)
            {
                globalVolume.SetActive(shouldBeActive);
                EditorUtility.SetDirty(globalVolume);
            }
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
                    File.Delete(finalPath);
                }

                File.Move(tempBundlePath, finalPath);

                platformFileNames[target.ToString()] = Path.GetFileNameWithoutExtension(finalPath);

                AssetDatabase.Refresh();

                Debug.Log($"Asset Bundle for {target} built and renamed to .mmworld{target} at: {finalPath}");
            }

            string packageJsonPath = Path.Combine(outputDirectory, "package.json");
            string jsonContent = JsonConvert.SerializeObject(new
            {
                pcFileName = platformFileNames.ContainsKey("StandaloneWindows") ? platformFileNames["StandaloneWindows"] : "defaultPCBundle",
                androidFileName = platformFileNames.ContainsKey("Android") ? platformFileNames["Android"] : "defaultAndroidBundle",
                usePoPr = usePostProccessing
            }, Formatting.Indented);

            File.WriteAllText(packageJsonPath, jsonContent);
            Debug.Log($"package.json created at: {packageJsonPath}");

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

        bool zipExists = File.Exists(zipFilePath);

        using (var zip = zipExists ? ZipFile.Open(zipFilePath, ZipArchiveMode.Update) : ZipFile.Open(zipFilePath, ZipArchiveMode.Create))
        {
            zip.CreateEntryFromFile(bundlePath, Path.GetFileName(bundlePath), CompressionLevel.Optimal);

            string[] platformBundles = Directory.GetFiles(outputDirectory, $"*.mmworld{target}");
            foreach (var platformBundle in platformBundles)
            {
                zip.CreateEntryFromFile(platformBundle, Path.GetFileName(platformBundle), CompressionLevel.Optimal);
            }

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

        File.Delete(bundlePath);

        Debug.Log($"Asset bundle and package.json zipped to: {zipFilePath}");
    }
}
