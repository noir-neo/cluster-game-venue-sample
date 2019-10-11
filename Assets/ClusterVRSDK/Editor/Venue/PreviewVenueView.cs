using System.IO;
using ClusterVRSDK.Core.Editor;
using ClusterVRSDK.Core.Editor.Venue;
using UnityEditor;
using UnityEngine;

namespace ClusterVRSDK.Editor.Venue
{
    public class PreviewVenueView
    {
        readonly UploadVenueDataStore dataStore;

        public PreviewVenueView(UploadVenueDataStore dataStore)
        {
            this.dataStore = dataStore;
        }

        bool executeBuild;
        string errorMessage;
        bool executePreview;
        bool executePreviousPreview;

        public void Process()
        {
            if (executePreview || executePreviousPreview)
            {
                var previous = executePreviousPreview;
                executePreview = false;
                executePreviousPreview = false;

                if (!VenueSdkTools.ValidateVenue(out errorMessage))
                {
                    Debug.LogError(errorMessage);
                    EditorUtility.DisplayDialog("ClusterVRSDK", errorMessage, "閉じる");
                    return;
                }

                if (!previous)
                {
                    EditorPrefsUtils.PreviousBuildSceneName = dataStore?.SelectVenue?.VenueId?.Value ?? "sceneName";
                    AssetExporter.PreparePreview(EditorPrefsUtils.PreviousBuildSceneName);
                }

                VenueSdkTools.PreviewVenue(EditorPrefsUtils.LastBuildPath, EditorPrefsUtils.PreviousBuildSceneName);

                errorMessage = "";
            }
        }

        public void DrawUI()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.Space();

                var assetBundleName = $"{EditorPrefsUtils.PreviousBuildSceneName}";
                var previousBuildPath = $"{Application.temporaryCachePath}/{EditorUserBuildSettings.activeBuildTarget}/{assetBundleName}";

                if (File.Exists(previousBuildPath))
                {
                    executePreviousPreview = GUILayout.Button("以前のビルドをプレビュー");
                }

                executePreview = GUILayout.Button("　プレビュー　");
            }

            EditorGUILayout.Space();

            if (File.Exists(EditorPrefsUtils.LastBuildPath))
            {
                var fileInfo = new FileInfo(EditorPrefsUtils.LastBuildPath);
                EditorGUILayout.LabelField($"日時：{fileInfo.LastWriteTime}");
                EditorGUILayout.LabelField($"サイズ：{(double) fileInfo.Length / (1024 * 1024):F2} MB"); // Byte => MByte

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"ビルドした会場名：{fileInfo.Name}");
                    if (GUILayout.Button("Copy"))
                    {
                        EditorGUIUtility.systemCopyBuffer = fileInfo.Name;
                    }
                }
            }

            EditorGUILayout.HelpBox("アップロードまたはプレビューを行うシーンを開いておいてください。", MessageType.Info);
        }
    }
}
