using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.IO;

public class BuildNRunPP : EditorWindow
{
    class BuildTargetGrouping
    {
        public BuildTarget target;
        public BuildTargetGroup group;
        public string extension;
        public BuildTargetGrouping(BuildTarget target, BuildTargetGroup group, string extension)
        {
            this.target = target;
            this.group = group;
            this.extension = extension;
        }
    }
    enum BuildTargets { None = 0, Windows = 1, Mac = 2, Linux = 4, Android = 8, iOS = 16 };
    private static BuildTargets target;

    private static Dictionary<BuildTargets, BuildTargetGrouping> buildDictionary;

    private static string buildName;

    private static readonly BuildTargets[] ignoredTargets = {BuildTargets.None};

    public delegate void OnBuildCompleted(string path);

    public static OnBuildCompleted onBuild;

    private static string langFileDirectory = "C:\\Users\\Gismo\\Documents\\Game Dev\\The Sandbox\\Lang";

    private void LoadBuildInfos()
    {
        buildDictionary = new Dictionary<BuildTargets, BuildTargetGrouping>();

        buildDictionary.Add(BuildTargets.Windows, new BuildTargetGrouping(
            BuildTarget.StandaloneWindows, BuildTargetGroup.Standalone, ".exe"));

        buildDictionary.Add(BuildTargets.Mac, new BuildTargetGrouping(
            BuildTarget.StandaloneOSX, BuildTargetGroup.Standalone, ".app"));

        buildDictionary.Add(BuildTargets.Linux, new BuildTargetGrouping(
            BuildTarget.StandaloneLinux64, BuildTargetGroup.Standalone, ".x86_64"));

        buildDictionary.Add(BuildTargets.Android, new BuildTargetGrouping(
            BuildTarget.Android, BuildTargetGroup.Android, ".apk"));

        buildDictionary.Add(BuildTargets.iOS, new BuildTargetGrouping(
            BuildTarget.iOS, BuildTargetGroup.iOS, ""));
    }

    [MenuItem("Gismo/BNRPP")]
    public static void ShowWindow()
    {
        GetWindow<BuildNRunPP>();
    }

    private void OnEnable()
    {
        onBuild += CopyLanguageFiles;
    }

    private void OnDisable()
    {
        onBuild -= CopyLanguageFiles;
    }

    private static void CopyLanguageFiles(string path)
    {
        if (File.Exists(Path.Combine(Path.GetDirectoryName(path), "Lang")))
            return;
        FileUtil.CopyFileOrDirectory(langFileDirectory, Path.Combine(Path.GetDirectoryName(path),"Lang"));
    }

    private void OnGUI()
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label("Build Name");
        buildName = GUILayout.TextField(buildName);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Build Targets");
        target = (BuildTargets)EditorGUILayout.EnumFlagsField(target);
        GUILayout.EndHorizontal();

        if (GUILayout.Button("Start Build Process"))
        {
            LoadBuildInfos();

            string path = EditorUtility.SaveFolderPanel("Where would you like to build the game?", "", "");

            if (string.IsNullOrEmpty(path))
                return;

            foreach (BuildTargets buildRequests in GetSelectedTargets(target))
            {
                Directory.CreateDirectory(Path.Combine(path, $"{buildRequests}"));
                string result = CreateBuild(Path.Combine(path, $"{buildRequests}", $"{buildName}{buildDictionary[buildRequests].extension}"), buildDictionary[buildRequests]);
            
                if(!string.IsNullOrEmpty(result))
                {
                    Debug.Log($"Build for {buildRequests} succeeded\n{result}");
                    onBuild?.Invoke(result);
                }
                else
                {
                    Debug.Log($"Build for {buildRequests} failed");
                }
            }
        }
    }

    private string CreateBuild(string filePath, BuildTargetGrouping grouping)
    {
        return CreateBuild(filePath, grouping.target, grouping.group);
    }

    private string CreateBuild(string filePath,BuildTarget target, BuildTargetGroup targetGroup)
    {
        if (!BuildPipeline.IsBuildTargetSupported(targetGroup, target))
        {
            Debug.LogError($"Unsupported target - {target}");
            return null;
        }

        BuildPlayerOptions options = new BuildPlayerOptions();
        List<string> scenes = new List<string>();

        foreach(EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
        {
            if (!string.IsNullOrEmpty(scene.path))
                scenes.Add(scene.path);
        }

        options.locationPathName = filePath;
        options.options = BuildOptions.None;

        options.target = target;
        options.targetGroup = targetGroup;

        options.scenes = scenes.ToArray();

        BuildReport report = BuildPipeline.BuildPlayer(options);

        if(report.summary.result == BuildResult.Succeeded)
        {
            return report.summary.outputPath;
        }
        return null;
    }

    private static BuildTargets[] GetSelectedTargets(BuildTargets sourceMask)
    {
        BuildTargets[] targets = BuildNRunPPHelpers<BuildTargets>.GetAllSelected(sourceMask, new List<BuildTargets>(ignoredTargets));
        if (targets.Length == 0)
            throw new Exception("No build targets have been selected");
        return targets;
    }
}
