using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using System;
using System.IO;
using System.Diagnostics;
using Debug = UnityEngine.Debug;

namespace Gismo.EditorExtensions
{

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

        private static int buildCount;

        enum BuildTargets { None = 0, Windows = 1, Mac = 2, Linux = 4, Android = 8, iOS = 16 };
        private static BuildTargets target;

        private static Dictionary<BuildTargets, BuildTargetGrouping> buildDictionary;

        private static string buildPostFix;

        private static readonly BuildTargets[] ignoredTargets = { BuildTargets.None };

        public delegate void OnBuildCompleted(string path);

        public static OnBuildCompleted onBuild;

        private static string langFileDirectory = "C:\\Users\\Gismo\\Documents\\Game Dev\\The Sandbox\\Lang";

        private const string BuildTargetKey = "BuildTarget";
        private const string BuildPostFixKey = "BuildPostFix";
        private const string BuildCountKey = "BuildCount";
        private const string BuildDirPathKey = "BuildDir";

        private const string GameName = "Project Quip";

        private static string lastDirectory = "";

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
                BuildTarget.iOS, BuildTargetGroup.iOS, "NOT YET IMPLEMENTED"));
        }

        [MenuItem("Gismo/BNRPP")]
        public static void ShowWindow()
        {
            BuildNRunPP window = GetWindow<BuildNRunPP>();
            window.titleContent = new GUIContent("Build and Run ++");
            LoadSettings();
        }

        private void OnEnable()
        {
            //onBuild += CopyLanguageFiles;

            LoadSettings();
        }

        private void OnDisable()
        {
            //onBuild -= CopyLanguageFiles;
            SaveSettings();
        }

        private static void LoadSettings()
        {
            target = (BuildTargets) EditorPrefs.GetInt(BuildTargetKey);
            buildPostFix = EditorPrefs.GetString(BuildPostFixKey);
            buildCount = EditorPrefs.GetInt(BuildCountKey);
            lastDirectory = EditorPrefs.GetString(BuildDirPathKey);
        }

        private static void SaveSettings()
        {
            EditorPrefs.SetInt(BuildTargetKey, (int)target);
            EditorPrefs.SetString(BuildPostFixKey, buildPostFix);
            EditorPrefs.SetInt(BuildCountKey, buildCount);
            EditorPrefs.SetString(BuildDirPathKey, lastDirectory);
        }

        private static void CopyLanguageFiles(string path)
        {
            if (File.Exists(Path.Combine(Path.GetDirectoryName(path), "Lang")))
                return;
            FileUtil.CopyFileOrDirectory(langFileDirectory, Path.Combine(Path.GetDirectoryName(path), "Lang"));
        }

        private void OnGUI()
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Build Postfix");
            buildPostFix = GUILayout.TextField(buildPostFix);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Build Targets");
            target = (BuildTargets)EditorGUILayout.EnumFlagsField(target);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Play and Run Count");
            buildCount = EditorGUILayout.IntSlider(buildCount, 0, 5);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Start Build Process"))
            {
                LoadBuildInfos();

                string path = EditorUtility.SaveFolderPanel("Where would you like to build the game?", lastDirectory, "");

                if (string.IsNullOrEmpty(path))
                    return;
                lastDirectory = path;
                SaveSettings();

                bool buildSucced = true;

                foreach (BuildTargets buildRequests in GetSelectedTargets(target))
                {
                    if(!InitalizeNCreateBuild(path, buildRequests))
                    {
                        buildSucced = false;
                    }
                }

                if (buildSucced)
                {
                    if (buildCount > 0)
                    {
                        RunWindowsBuild(path);
                    }
                }
            }
        }

        private static void RunWindowsBuild(string path)
        {
            if (Directory.Exists(Path.Combine(path, $"{BuildTargets.Windows}")))
            {
                string windowsBuildPath = Path.Combine(path, $"{BuildTargets.Windows}", $"{GameName}-{buildPostFix}{buildDictionary[BuildTargets.Windows].extension}");
                Debug.Log($"Found build to run x{buildCount} times\n{windowsBuildPath}");

                Process[] processes = new Process[buildCount];

                for (int i = 0; i < buildCount; i++)
                {
                    processes[i] = new Process();
                    processes[i].StartInfo.FileName = windowsBuildPath;
                }

                for (int i = 0; i < buildCount; i++)
                {
                    processes[i].Start();
                }
            }
        }

        private bool InitalizeNCreateBuild(string path, BuildTargets buildRequests)
        {
            Directory.CreateDirectory(Path.Combine(path, $"{buildRequests}"));
            string result = CreateBuild(Path.Combine(path, $"{buildRequests}", $"{GameName}-{buildPostFix}{buildDictionary[buildRequests].extension}"), buildDictionary[buildRequests]);

            if (!string.IsNullOrEmpty(result))
            {
                Debug.Log($"Build for {buildRequests} succeeded\n{result}");
                onBuild?.Invoke(result);
                return true;
            }
            else
            {
                Debug.Log($"Build for {buildRequests} failed");
                return false;
            }
        }

        private string CreateBuild(string filePath, BuildTargetGrouping grouping)
        {
            return CreateBuild(filePath, grouping.target, grouping.group);
        }

        private string CreateBuild(string filePath, BuildTarget target, BuildTargetGroup targetGroup)
        {
            if (!BuildPipeline.IsBuildTargetSupported(targetGroup, target))
            {
                Debug.LogError($"Unsupported target - {target}");
                return null;
            }

            BuildPlayerOptions options = new BuildPlayerOptions();
            List<string> scenes = new List<string>();

            foreach (EditorBuildSettingsScene scene in EditorBuildSettings.scenes)
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

            if (report.summary.result == BuildResult.Succeeded)
            {
                return report.summary.outputPath;
            }
            return null;
        }

        private static BuildTargets[] GetSelectedTargets(BuildTargets sourceMask)
        {
            BuildTargets[] targets = EnumTools<BuildTargets>.GetAllSelected(sourceMask, new List<BuildTargets>(ignoredTargets));
            if (targets.Length == 0)
                throw new Exception("No build targets have been selected");
            return targets;
        }
    }
}