using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
namespace Gismo.EditorExtensions
{
    public class GismoLoad : EditorWindow
    {
        private static string sceneFolderCleanUpTag;
        private static string lastSceneFolderCleanupTag;
        private static Vector2 scrollPosition = Vector2.zero;
        private static Dictionary<string, List<string>> scenes = new Dictionary<string, List<string>>();

        private static bool doSceneFolderCleanup;

        private static Texture2D addIcon;
        private static Texture2D findIcon;

        private static Texture2D removeIcon;

        [MenuItem("Gismo/OpenScene")]
        static void OpenSceneWindow()
        {
            GismoLoad window = GetWindow<GismoLoad>();
            window.titleContent = new GUIContent("Gismo Load");

            Rect position = window.position;
            Rect screen = new Rect(0f, 0f, Screen.currentResolution.width, Screen.currentResolution.height);
            position.center = screen.center;
            window.position = position;

            doSceneFolderCleanup = EditorPrefs.GetBool("DoSceneFolderCleanup");

            if (doSceneFolderCleanup)
            {
                sceneFolderCleanUpTag = EditorPrefs.GetString("SceneFolderStart");
            }
            window.Show();
        }

        static void Init()
        {   
            addIcon = EditorGUIUtility.FindTexture("Toolbar Plus");
            findIcon = EditorGUIUtility.FindTexture("ToolsIcon");
            removeIcon = EditorGUIUtility.FindTexture("TreeEditor.Trash");
        }

        void OnGUI()
        {
            if(addIcon == null || findIcon == null || removeIcon == null)
            {
                Init();
            }
            FindSceneInProject();
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);
            GUILayout.BeginHorizontal();
            lastSceneFolderCleanupTag = EditorGUILayout.TextField("Scene Folder Start Tag", lastSceneFolderCleanupTag);

            if (GUILayout.Button("Apply"))
            {
                if (lastSceneFolderCleanupTag != sceneFolderCleanUpTag)
                {
                    sceneFolderCleanUpTag = lastSceneFolderCleanupTag;

                    if (sceneFolderCleanUpTag == "")
                    {
                        EditorPrefs.SetBool("DoSceneFolderCleanup", false);
                        doSceneFolderCleanup = false;
                    }
                    else
                    {
                        EditorPrefs.SetBool("DoSceneFolderCleanup", true);
                        doSceneFolderCleanup = true;
                        EditorPrefs.SetString("SceneFolderStart", sceneFolderCleanUpTag);
                    }
                }
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginVertical();

            DisplayLoadedScenes();

            foreach (KeyValuePair<string, List<string>> kvp in scenes)
            {
                DisplayFolder(kvp.Key, kvp.Value);
            }

            GUILayout.Space(20);
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
        }

        private void FindSceneInProject()
        {
            scenes.Clear();

            string[] guids;

            guids = AssetDatabase.FindAssets("t:Scene");

            for (int i = 0; i < guids.Length; i++)
            {
                guids[i] = AssetDatabase.GUIDToAssetPath(guids[i]);
                string scenefolderName = Path.GetFileName(Path.GetDirectoryName(guids[i]));
                if (doSceneFolderCleanup)
                {
                    if (scenefolderName.Contains(sceneFolderCleanUpTag))
                    {
                        if (!scenes.ContainsKey(scenefolderName))
                        {
                            scenes.Add(scenefolderName, new List<string>());
                        }

                        scenes[scenefolderName].Add(guids[i]);
                    }
                }
                else
                {

                    if (!scenes.ContainsKey(scenefolderName))
                    {
                        scenes.Add(scenefolderName, new List<string>());
                    }

                    scenes[scenefolderName].Add(guids[i]);
                }

            }
        }

        private void DisplayLoadedScenes()
        {
            if (EditorSceneManager.loadedSceneCount == 1) return;
            GUILayout.Label("LOADED SCENES");

            for (int i = 0; i < EditorSceneManager.loadedSceneCount; i++)
            {
                Scene currentScene = EditorSceneManager.GetSceneAt(i);

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("SET ACTIVE " + currentScene.name.ToUpper()))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.SetActiveScene(currentScene);
                    }
                }

                if (GUILayout.Button(removeIcon))
                {
                    if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    {
                        EditorSceneManager.CloseScene(currentScene, true);
                    }
                }


                GUILayout.EndHorizontal();
            }
        }

        private void DisplayFolder(string folderName, List<string> scenePathList)
        {
            if (doSceneFolderCleanup)
            {
                GUILayout.Label(folderName.ToUpper().Replace(sceneFolderCleanUpTag, ""));
            }
            else
            {
                GUILayout.Label(folderName.ToUpper());
            }

            foreach (string scenePath in scenePathList)
            {
                DisplayScene(scenePath);
            }

            GUILayout.Space(10);
        }

        private void DisplayScene(string scenePath)
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("OPEN " + Path.GetFileNameWithoutExtension(scenePath).ToUpper(),GUILayout.MinWidth(200)))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                }
            }
            if (GUILayout.Button(addIcon))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
                }
            }

            if(GUILayout.Button(findIcon))
            {
                Selection.activeObject = AssetDatabase.LoadMainAssetAtPath(scenePath);
                EditorGUIUtility.PingObject(Selection.activeObject);
            }

            GUILayout.EndHorizontal();
        }
    }
}