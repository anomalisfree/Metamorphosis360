using System;
using System.IO;
using System.Xml;
using UnityEngine;

namespace Main.Core
{
    /// <summary>
    /// Reads Firebase configuration from GoogleService-Info.plist.
    /// Works in both Editor and Runtime.
    /// </summary>
    public static class FirebaseConfigReader
    {
        private const string PLIST_FILENAME = "GoogleService-Info.plist";
        
        private static string _cachedDatabaseUrl;
        private static string _cachedProjectId;
        private static string _cachedStorageBucket;
        private static string _cachedApiKey;
        private static bool _isLoaded;

        public static string DatabaseUrl
        {
            get
            {
                EnsureLoaded();
                return _cachedDatabaseUrl;
            }
        }

        public static string ProjectId
        {
            get
            {
                EnsureLoaded();
                return _cachedProjectId;
            }
        }

        public static string StorageBucket
        {
            get
            {
                EnsureLoaded();
                return _cachedStorageBucket;
            }
        }

        public static string ApiKey
        {
            get
            {
                EnsureLoaded();
                return _cachedApiKey;
            }
        }

        private static void EnsureLoaded()
        {
            if (_isLoaded)
                return;

            LoadConfig();
            _isLoaded = true;
        }

        private static void LoadConfig()
        {
            var plistPath = GetPlistPath();
            
            if (string.IsNullOrEmpty(plistPath) || !File.Exists(plistPath))
            {
                Debug.LogError($"[FirebaseConfigReader] Could not find {PLIST_FILENAME} at path: {plistPath}");
                return;
            }

            try
            {
                var plistContent = File.ReadAllText(plistPath);
                ParsePlist(plistContent);
                Debug.Log($"[FirebaseConfigReader] Loaded config from {plistPath}");
            }
            catch (Exception e)
            {
                Debug.LogError($"[FirebaseConfigReader] Failed to read {PLIST_FILENAME}: {e.Message}");
            }
        }

        private static string GetPlistPath()
        {
#if UNITY_EDITOR
            // In Editor, the file is in Assets folder
            return Path.Combine(Application.dataPath, PLIST_FILENAME);
#elif UNITY_IOS
            // On iOS, the file is copied to the app bundle
            return Path.Combine(Application.streamingAssetsPath, PLIST_FILENAME);
#else
            // On other platforms, try StreamingAssets first, then dataPath
            var streamingPath = Path.Combine(Application.streamingAssetsPath, PLIST_FILENAME);
            if (File.Exists(streamingPath))
                return streamingPath;
            return Path.Combine(Application.dataPath, PLIST_FILENAME);
#endif
        }

        private static void ParsePlist(string plistContent)
        {
            var doc = new XmlDocument();
            doc.LoadXml(plistContent);

            var dict = doc.SelectSingleNode("//dict");
            if (dict == null)
            {
                Debug.LogError("[FirebaseConfigReader] Invalid plist format: no dict element found");
                return;
            }

            var children = dict.ChildNodes;
            string currentKey = null;

            foreach (XmlNode node in children)
            {
                if (node.Name == "key")
                {
                    currentKey = node.InnerText;
                }
                else if (currentKey != null && (node.Name == "string" || node.Name == "true" || node.Name == "false"))
                {
                    var value = node.InnerText;
                    
                    switch (currentKey)
                    {
                        case "DATABASE_URL":
                            _cachedDatabaseUrl = value;
                            break;
                        case "PROJECT_ID":
                            _cachedProjectId = value;
                            break;
                        case "STORAGE_BUCKET":
                            _cachedStorageBucket = value;
                            break;
                        case "API_KEY":
                            _cachedApiKey = value;
                            break;
                    }
                    
                    currentKey = null;
                }
            }
        }

        /// <summary>
        /// Force reload the configuration (useful if the file was modified).
        /// </summary>
        public static void Reload()
        {
            _isLoaded = false;
            _cachedDatabaseUrl = null;
            _cachedProjectId = null;
            _cachedStorageBucket = null;
            _cachedApiKey = null;
            EnsureLoaded();
        }
    }
}
