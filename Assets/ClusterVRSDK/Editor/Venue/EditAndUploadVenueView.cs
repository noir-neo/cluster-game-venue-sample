
using ClusterVRSDK.Core.Editor.Venue;
using UnityEditor;
using UnityEngine;

namespace ClusterVRSDK.Editor.Venue
{
    public class EditAndUploadVenueView
    {
        readonly UploadVenueDataStore dataStore;
        readonly EditVenueView editVenueView;
        readonly UploadVenueView uploadVenueView;

        public EditAndUploadVenueView(UploadVenueDataStore dataStore)
        {
            this.dataStore = dataStore;
            editVenueView = new EditVenueView(dataStore);
            uploadVenueView = new UploadVenueView(dataStore);
        }

        public void Process()
        {
            if (dataStore.SelectVenue == null)
            {
                return;
            }

            editVenueView.Process();
            uploadVenueView.Process();
        }

        enum Tab
        {
            Edit,
            Upload
        }

        Tab currentTab;

        public void DrawUI(EditorWindow parent)
        {
            if (dataStore.SelectVenue == null)
            {
                return;
            }

            EditorGUILayout.Space();
            GUILayout.Box("", GUILayout.Width(parent.position.width - 10), GUILayout.Height(1));
            EditorGUILayout.Space();

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                // タブを描画する
                //Styles.TabToggles, Styles.TabButtonStyle, Styles.TabButtonSize
                currentTab = (Tab) GUILayout.Toolbar((int) currentTab, new[] {"会場の設定", "アップロード"});
                GUILayout.FlexibleSpace();
            }

            if (currentTab == Tab.Edit)
            {
                editVenueView.DrawUI(parent);
            }
            else
            {
                uploadVenueView.DrawUI();
            }
        }
    }
}
