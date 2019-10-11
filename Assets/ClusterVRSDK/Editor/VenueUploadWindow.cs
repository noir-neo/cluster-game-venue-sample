using ClusterVRSDK.Core.Editor.Venue;
using ClusterVRSDK.Editor.Venue;
using UnityEditor;
using UnityEngine;

namespace ClusterVRSDK.Editor
{
    public class VenueUploadWindow : SdkBaseUiWindow
    {
        [MenuItem("clusterSDK/VenueUpload")]
        public static void Open()
        {
            var window = GetWindow<VenueUploadWindow>();
            window.titleContent = new GUIContent("cluster UploadVenueWindow");
        }

        UploadVenueDataStore dataStore;
        SelectVenueView selectVenue;
        EditAndUploadVenueView editAndUploadView;
        PreviewVenueView previewVenueView;

        void OnEnable()
        {
            dataStore = new UploadVenueDataStore();
            selectVenue = new SelectVenueView(dataStore);
            editAndUploadView = new EditAndUploadVenueView(dataStore);
            previewVenueView = new PreviewVenueView(dataStore);
        }

        void OnDisable()
        {
            dataStore = null;
            selectVenue = null;
            editAndUploadView = null;
            previewVenueView = null;
        }

        Vector2 scrollPosition = Vector2.zero;

        void OnGUI()
        {
            ShowTokenSettingUI();
            EditorGUILayout.Space();

            if (!IsLoggedIn)
            {
                return;
            }

            dataStore.AccessToken = VerifiedToken;
            selectVenue.Process();
            editAndUploadView.Process();
            previewVenueView.Process();

            using (var scrollViewScope = new GUILayout.ScrollViewScope(scrollPosition))
            {
                scrollPosition = scrollViewScope.scrollPosition;
                selectVenue.DrawUI(this);
                editAndUploadView.DrawUI(this);

                GUILayout.FlexibleSpace();
            }

            previewVenueView.DrawUI();
        }
    }
}
