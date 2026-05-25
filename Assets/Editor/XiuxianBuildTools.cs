using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class XiuxianBuildTools
{
    private const string ProductName = "Xiuxian Fitness";
    private const string CompanyName = "Kongdao";
    private const string PackageName = "com.kongdao.xiuxianfitness";
    private const string MainScene = "Assets/Scenes/Main.unity";

    [InitializeOnLoadMethod]
    private static void ConfigureOnLoad()
    {
        if (SessionState.GetBool("XiuxianBuildConfigured", false)) return;
        SessionState.SetBool("XiuxianBuildConfigured", true);
        ConfigureBuildSettings();
    }

    [MenuItem("Xiuxian/Configure Build Settings")]
    public static void ConfigureBuildSettings()
    {
        PlayerSettings.companyName = CompanyName;
        PlayerSettings.productName = ProductName;
        PlayerSettings.defaultScreenWidth = 390;
        PlayerSettings.defaultScreenHeight = 844;
        PlayerSettings.defaultIsNativeResolution = false;
        PlayerSettings.runInBackground = false;
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Standalone, PackageName);
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, PackageName);
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, PackageName);

        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel23;
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainScene, true)
        };

        AssetDatabase.SaveAssets();
        Debug.Log("Xiuxian build settings configured.");
    }

    [MenuItem("Xiuxian/Build/Windows Demo")]
    public static void BuildWindowsDemo()
    {
        ConfigureBuildSettings();
        string outputPath = Path.Combine("Builds", "Windows", "XiuxianFitness.exe");
        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { MainScene },
            locationPathName = outputPath,
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("Windows build result: " + report.summary.result + " -> " + outputPath);
    }

    [MenuItem("Xiuxian/Build/Android APK")]
    public static void BuildAndroidApk()
    {
        ConfigureBuildSettings();
        string outputPath = Path.Combine("Builds", "Android", "XiuxianFitness.apk");
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));

        BuildPlayerOptions options = new BuildPlayerOptions
        {
            scenes = new[] { MainScene },
            locationPathName = outputPath,
            target = BuildTarget.Android,
            options = BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        Debug.Log("Android build result: " + report.summary.result + " -> " + outputPath);
    }
}
