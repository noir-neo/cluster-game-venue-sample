using System.Linq;
using ClusterVRSDK.Core.Editor.Venue;
using ClusterVRSDK.Core.Editor.Venue.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ClusterVRSDK.Editor.Venue
{
    public class EditVenueView
    {
        readonly UploadVenueDataStore dataStore;
        readonly DrawThumbnailView drawThumbnailView;

        public EditVenueView(UploadVenueDataStore dataStore)
        {
            this.dataStore = dataStore;
            this.dataStore.EditVenue = new EditVenue();
            drawThumbnailView = new DrawThumbnailView();
        }

        bool executeSaveVenue;
        bool savingVenueThumbnail;

        string errorMessage;

        public void Process()
        {
            if (dataStore.SelectVenue == null)
            {
                errorMessage = null;
                return;
            }

            if (executeSaveVenue)
            {
                executeSaveVenue = false;
                savingVenueThumbnail = true;

                var editVenue = dataStore.EditVenue;

                var patchVenuePayload = new PatchVenuePayload
                {
                    description = editVenue.Description,
                    name = editVenue.Name,
                    thumbnailUrls = dataStore.SelectVenue.ThumbnailUrls.ToList()
                };

                var patchVenueService =
                    new PatchVenueSettingService(
                        dataStore.AccessToken,
                        dataStore.SelectVenue.VenueId,
                        patchVenuePayload,
                        editVenue.ThumbnailPath,
                        venue =>
                        {
                            var list = dataStore.VenueMap[venue.Group.Id].List;
                            var index = list.FindIndex(x => x.VenueId == venue.VenueId);
                            list[index] = venue;
                            dataStore.SelectVenue = venue;
                            dataStore.EditVenue = null;
                            savingVenueThumbnail = false;
                        },
                        exception =>
                        {
                            errorMessage = $"会場情報の保存に失敗しました。{exception.Message}";
                            savingVenueThumbnail = false;
                        });
                patchVenueService.Run();
                errorMessage = null;
            }
        }

        public void DrawUI(EditorWindow parent)
        {
            if (dataStore.SelectVenue == null)
            {
                return;
            }

            EditorGUILayout.Space();

            var selectVenue = dataStore.SelectVenue;
            var editVenue = dataStore.EditVenue ?? (dataStore.EditVenue = new EditVenue());

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("名前");
                editVenue.Name = EditorGUILayout.TextField(editVenue.Name ?? selectVenue.Name);
            }

            EditorGUILayout.LabelField("説明");
            var textAreaOption = new[] {GUILayout.MinHeight(64)};
            editVenue.Description = EditorGUILayout.TextArea(editVenue.Description ?? selectVenue.Description, textAreaOption);

            if (string.IsNullOrEmpty(editVenue.ThumbnailPath) && selectVenue.ThumbnailUrls.Any())
            {
                drawThumbnailView.OverwriteDownloadUrl(selectVenue.ThumbnailUrls.First(x => x != null));
            }

            drawThumbnailView.DrawUI(savingVenueThumbnail);

            if (GUILayout.Button("サムネイル画像を選択..."))
            {
                editVenue.ThumbnailPath =
                    EditorUtility.OpenFilePanelWithFilters(
                        "画像を選択",
                        "",
                        new[] {"Image files", "png,jpg,jpeg", "All files", "*"}
                    );
                drawThumbnailView.OverwriteFilePath(editVenue.ThumbnailPath);
            }

            EditorGUILayout.Space();


            if (!savingVenueThumbnail)
            {
                executeSaveVenue = GUILayout.Button("保存");
            }

            if (!string.IsNullOrEmpty(errorMessage))
            {
                EditorGUILayout.HelpBox(errorMessage, MessageType.Error);
            }
        }
    }
}
