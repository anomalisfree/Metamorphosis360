#if UNITY_IOS
using System.IO;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;

namespace Metamorphosis360.Main.Editor
{
	public static class IOSLocationPlistPostProcessor
	{
		private const string WhenInUseKey = "NSLocationWhenInUseUsageDescription";
		private const string DefaultWhenInUseText = "Нужно для отображения вашего положения (GPS) на карте.";

		[PostProcessBuild(1001)]
		public static void OnPostProcessBuild(BuildTarget target, string pathToBuiltProject)
		{
			if (target != BuildTarget.iOS)
			{
				return;
			}

			var plistPath = Path.Combine(pathToBuiltProject, "Info.plist");
			if (!File.Exists(plistPath))
			{
				UnityEngine.Debug.LogWarning($"[IOSLocationPlistPostProcessor] Info.plist not found at: {plistPath}");
				return;
			}

			var plist = new PlistDocument();
			plist.ReadFromFile(plistPath);
			var root = plist.root;

			if (!root.values.ContainsKey(WhenInUseKey))
			{
				root.SetString(WhenInUseKey, DefaultWhenInUseText);
				plist.WriteToFile(plistPath);
				UnityEngine.Debug.Log("[IOSLocationPlistPostProcessor] Added NSLocationWhenInUseUsageDescription.");
			}
		}
	}
}
#endif
