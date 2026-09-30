using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ValourGames.Shawarma.EditorTools
{
    /// <summary>
    /// Меню "Valour Games" в верхней панели Unity.
    /// "Собрать игру" создаёт сцену Main, настраивает проект под Яндекс Игры и открывает сцену.
    /// </summary>
    public static class ValourGamesMenu
    {
        const string SceneDir = "Assets/_Game/Scenes";
        const string ScenePath = SceneDir + "/Main.unity";
        const string BuildDir = "Builds/WebGL";

        [MenuItem("Valour Games/Собрать игру", priority = 0)]
        public static void BuildScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            Directory.CreateDirectory(SceneDir);
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.12f, 0.08f, 0.05f);
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();

            new GameObject("Game").AddComponent<Game>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };

            ApplyProjectSettings();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Valour Games",
                "Сцена собрана и настройки применены.\n\n" +
                "В окне Game выбери разрешение 1920x1080 (Full HD, горизонтально) и нажми Play.",
                "Поехали");
        }

        [MenuItem("Valour Games/Применить настройки WebGL", priority = 20)]
        public static void ApplyProjectSettings()
        {
            PlayerSettings.companyName = "Valour Games";
            PlayerSettings.productName = "Shawarma Tycoon";
            PlayerSettings.runInBackground = false;

            PlayerSettings.defaultWebScreenWidth = 960;
            PlayerSettings.defaultWebScreenHeight = 540;
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;   // работает на любом хостинге, без настройки заголовков
            PlayerSettings.WebGL.dataCaching = true;
            PlayerSettings.WebGL.nameFilesAsHashes = false;      // имена файлов латиницей, без пробелов
            // шаблон страницы с SDK Яндекс Игр (Assets/WebGLTemplates/YandexGames)
            if (Directory.Exists("Assets/WebGLTemplates/YandexGames"))
                PlayerSettings.WebGL.template = "PROJECT:YandexGames";
            else
                Debug.LogWarning("[Valour Games] Нет папки Assets/WebGLTemplates/YandexGames — распакуй её из архива в Assets!");

            Debug.Log("[Valour Games] Настройки WebGL применены");
        }

        [MenuItem("Valour Games/Сделать WebGL-билд", priority = 40)]
        public static void BuildWebGL()
        {
            if (!File.Exists(ScenePath))
            {
                EditorUtility.DisplayDialog("Valour Games", "Сначала нажми «Valour Games → Собрать игру».", "Ок");
                return;
            }
            ApplyProjectSettings();
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = BuildDir,
                target = BuildTarget.WebGL,
                options = BuildOptions.None
            };
            var report = UnityEditor.BuildPipeline.BuildPlayer(opts);
            if (report.summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
            {
                EditorUtility.RevealInFinder(BuildDir + "/index.html");
                EditorUtility.DisplayDialog("Valour Games",
                    "Билд готов: папка " + BuildDir + "\n\nДля Яндекса запакуй СОДЕРЖИМОЕ этой папки (index.html и папку Build) в zip — index.html должен лежать в корне архива.",
                    "Ок");
            }
            else
            {
                // собираем реальные причины из отчёта сборки — они не всегда попадают в Console
                var sb = new System.Text.StringBuilder();
                sb.AppendLine("Результат: " + report.summary.result + ", ошибок: " + report.summary.totalErrors);
                foreach (var step in report.steps)
                    foreach (var m in step.messages)
                        if (m.type == LogType.Error || m.type == LogType.Exception)
                        {
                            sb.AppendLine("• " + m.content);
                            Debug.LogError("[Valour Games] " + step.name + ": " + m.content);
                        }
                string text = sb.ToString();
                if (text.Length > 1500) text = text.Substring(0, 1500) + "...";
                Debug.LogError("[Valour Games] Сборка не удалась.\n" + text);
                EditorUtility.DisplayDialog("Valour Games", "Сборка не удалась.\n\n" + text +
                    "\nПопробуй собрать через File → Build Profiles → Build (папка Builds/WebGL).", "Ок");
            }
        }
    }
}
