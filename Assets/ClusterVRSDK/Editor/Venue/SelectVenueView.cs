using System.Collections.Generic;
using System.Linq;
using ClusterVRSDK.Core.Editor.Venue;
using ClusterVRSDK.Core.Editor.Venue.Json;
using DepthFirstScheduler;
using UnityEditor;
using UnityEngine;

namespace ClusterVRSDK.Editor.Venue
{
    public class SelectVenueView
    {
        readonly UploadVenueDataStore dataStore;
        readonly DrawThumbnailView drawThumbnailView;
        private bool callingGetGroups;
        private HashSet<GroupID> callingGetVenue = new HashSet<GroupID>();

        public SelectVenueView(UploadVenueDataStore dataStore)
        {
            this.dataStore = dataStore;
            this.dataStore.GroupsIsDirty = true;
            drawThumbnailView = new DrawThumbnailView();
        }

        public void Process()
        {
            if (dataStore.AccessToken == null)
            {
                return;
            }

            if (dataStore.GroupsIsDirty && !callingGetGroups)
            {
                callingGetGroups = true;
                var _ = APIServiceClient.GetGroups.CallWithCallback(Empty.Value, dataStore.AccessToken,
                    groups =>
                    {
                        callingGetGroups = false;

                        dataStore.Groups = groups;
                        dataStore.GroupsIsDirty = false;
                        if (dataStore.Groups.List.Any())
                        {
                            dataStore.SelectGroup = dataStore.Groups.List[0];
                        }
                    },
                    exception =>
                    {
                        callingGetGroups = false;
                        dataStore.GroupsIsDirty = true;
                    });
            }

            var copiedMap = new Dictionary<GroupID, bool>(dataStore.VenueDirtyMap);
            foreach (var dirtyPair in copiedMap)
            {
                var groupId = dirtyPair.Key;
                if (dirtyPair.Value && !callingGetVenue.Contains(groupId))
                {
                    callingGetVenue.Add(groupId);
                    var _ = APIServiceClient.GetGroupVenues.CallWithCallback(groupId, dataStore.AccessToken,
                        venues =>
                        {
                            callingGetVenue.Remove(groupId);

                            dataStore.VenueMap[dirtyPair.Key] = venues;
                            dataStore.VenueDirtyMap[dirtyPair.Key] = false;
                        },
                        exception =>
                        {
                            callingGetVenue.Remove(groupId);

                            dataStore.VenueDirtyMap[dirtyPair.Key] = true;
                        });
                }
            }

            if (dataStore.SelectGroup != null && !dataStore.VenueMap.ContainsKey(dataStore.SelectGroup.Id))
            {
                dataStore.VenueDirtyMap[dataStore.SelectGroup.Id] = true;
            }

            if (executeNewVenue && dataStore.SelectGroup != null)
            {
                executeNewVenue = false;

                var newVenuePayload = new PostNewVenuePayload
                {
                    description = "説明未設定",
                    name = "NewVenue",
                    groupId = dataStore.SelectGroup.Id.Value
                };

                var postVenueService =
                    new PostRegisterNewVenueService(
                        dataStore.AccessToken,
                        newVenuePayload,
                        venue =>
                        {
                            var venueList = dataStore.VenueMap[venue.Group.Id].List;
                            venueList.Add(venue);
                            dataStore.SelectVenue = venue;
                            dataStore.EditVenue = null;
                            venueIdIndex = venueList.Count - 1;
                        },
                        exception => errorMessageRegisterVenue = $"新規会場の登録ができませんでした。{exception.Message}");
                postVenueService.Run();
                errorMessageRegisterVenue = null;
            }
        }

        int teamIdIndex;
        int venueIdIndex;
        bool executeNewVenue;
        string errorMessageRegisterVenue;

        public void DrawUI(EditorWindow parent)
        {
            if (dataStore.Groups != null && dataStore.Groups.List.Count == 0)
            {
                EditorGUILayout.HelpBox("clusterにてチーム登録をお願いいたします", MessageType.Warning);
                return;
            }

            Venues venues = null;
            if (dataStore.VenueMap != null && dataStore.SelectGroup != null)
            {
                dataStore.VenueMap.TryGetValue(dataStore.SelectGroup.Id, out venues);
            }

            if (dataStore.Groups != null && dataStore.SelectGroup != null && venues != null)
            {
                var teamOptions = dataStore.Groups.List.Select(x => x.Name).ToArray();
                var currentIndex = EditorGUILayout.Popup("所属チーム", teamIdIndex, teamOptions);

                if (currentIndex != teamIdIndex)
                {
                    teamIdIndex = currentIndex;

                    dataStore.SelectGroup = dataStore.Groups.List[teamIdIndex];
                    dataStore.SelectVenue = null;
                    dataStore.EditVenue = null;
                    venueIdIndex = 0;
                    return;
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    var venueOptions = venues.List.Select(x => x.Name).ToArray();
                    var currentVenueIndex = EditorGUILayout.Popup("会場一覧", venueIdIndex, venueOptions);

                    if (currentVenueIndex != venueIdIndex)
                    {
                        dataStore.SelectVenue = null;
                        dataStore.EditVenue = null;
                    }

                    if (venues.List.Any())
                    {
                        venueIdIndex = currentVenueIndex;
                        dataStore.SelectVenue = venues.List[venueIdIndex];
                    }

                    using (new EditorGUI.DisabledScope(venues.List.Exists(x => x.Name == "NewVenue")))
                    {
                        executeNewVenue = GUILayout.Button("新規会場追加");
                    }
                }
            }
            else
            {
                EditorGUILayout.HelpBox("会場情報を取得しています...", MessageType.None);
            }

            if (errorMessageRegisterVenue != null)
            {
                EditorGUILayout.HelpBox(errorMessageRegisterVenue, MessageType.Error);
            }

            if (dataStore.SelectVenue != null)
            {
                var selectVenue = dataStore.SelectVenue;

                EditorGUILayout.LabelField("説明");
                EditorGUILayout.HelpBox(selectVenue?.Description, MessageType.None);

                if (selectVenue.ThumbnailUrls.Any())
                {
                    drawThumbnailView.OverwriteDownloadUrl(selectVenue.ThumbnailUrls.First(x => x != null));
                }

                drawThumbnailView.DrawUI(false);
            }
        }
    }
}
