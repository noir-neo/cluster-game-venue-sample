using System.Collections.Generic;
using ClusterVRSDK.Core.Editor;
using ClusterVRSDK.Core.Editor.Venue;
using DepthFirstScheduler;
using UnityEditor;
using UnityEngine;

namespace ClusterVRSDK.Editor
{
    public abstract class SdkBaseUiWindow : EditorWindow
    {
        // token
        string settedToken;
        internal string VerifiedToken { get; private set; }

        // login
        internal string Username { get; private set; }
        internal bool IsLoggedIn { get; private set; }
        bool isLoggingIn;

        // messasge
        internal readonly List<Message> messages = new List<Message>();

        internal struct Message
        {
            public Message(string body, MessageType messageType)
            {
                Body = body;
                MessageType = messageType;
            }

            public string Body { get; }

            public MessageType MessageType { get; }
        }

        internal void CreateMessage(Message message)
        {
            messages.Add(message);
        }

        internal void ShowTokenSettingUI()
        {
            EditorGUILayout.LabelField("Token", EditorStyles.boldLabel);
            settedToken = EditorGUILayout.TextField("API access token", settedToken);

            if (string.IsNullOrEmpty(settedToken) && !string.IsNullOrEmpty(EditorPrefsUtils.SavedAccessToken))
            {
                settedToken = EditorPrefsUtils.SavedAccessToken;
            }

            Login();

            if (GUILayout.Button("Get API access token"))
            {
                Application.OpenURL(Constants.WebBaseUrl + "/app/my/tokens");
            }

            EditorGUILayout.Space();
            GUILayout.Box("", GUILayout.Width(position.width), GUILayout.Height(1));
        }

        void Login()
        {
            var tokenChanged = VerifiedToken != settedToken;
            if (IsLoggedIn && !string.IsNullOrEmpty(Username) && !tokenChanged)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Logged in as " + "\"" + Username + "\"",
                    EditorStyles.centeredGreyMiniLabel);
                EditorGUILayout.Space();
                return;
            }

            if (string.IsNullOrEmpty(settedToken))
            {
                Username = "";
                IsLoggedIn = false;
                CreateMessage(new Message("Please set access token", MessageType.Info));
                VerifiedToken = settedToken;
                return;
            }

            if (settedToken.Length != 64)
            {
                Username = "";
                IsLoggedIn = false;
                CreateMessage(new Message("Invalid token", MessageType.Error));
                VerifiedToken = settedToken;
                return;
            }

            if (!tokenChanged)
            {
                return;
            }

            if (isLoggingIn)
            {
                return;
            }

            VerifiedToken = settedToken;
            EditorPrefsUtils.SavedAccessToken = settedToken;
            GetUsername();
        }

        void GetUsername()
        {
            isLoggingIn = true;
            var _ = APIServiceClient.GetMyUser.CallWithCallback(Empty.Value, VerifiedToken, user =>
            {
                Username = user.Username;
                IsLoggedIn = true;
                isLoggingIn = false;
                Repaint();
            }, exc =>
            {
                isLoggingIn = false;
                EditorPrefsUtils.SavedAccessToken = "";
            }, 3);
        }
    }
}
