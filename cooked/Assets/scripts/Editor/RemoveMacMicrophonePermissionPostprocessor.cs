using System.IO;
using System.Diagnostics;
using System.Xml;
using UnityEditor;
using UnityEditor.Callbacks;

public static class RemoveMacMicrophonePermissionPostprocessor
{
    private const string MicrophoneUsageKey = "NSMicrophoneUsageDescription";
    private const string AudioInputEntitlementKey = "com.apple.security.device.audio-input";

    [PostProcessBuild(9999)]
    public static void RemoveMicrophoneUsageDescription(BuildTarget target, string pathToBuiltProject)
    {
        if (target != BuildTarget.StandaloneOSX)
        {
            return;
        }

        string appPath = GetAppPath(pathToBuiltProject);
        if (string.IsNullOrEmpty(appPath))
        {
            return;
        }

        RemoveKeyFromPlist(Path.Combine(appPath, "Contents/Info.plist"), MicrophoneUsageKey);
        RemoveKeyFromPlistsInFolder(Path.Combine(appPath, "Contents"), AudioInputEntitlementKey);
        ResignAdHoc(appPath);
    }

    private static string GetAppPath(string pathToBuiltProject)
    {
        if (Directory.Exists(pathToBuiltProject) && pathToBuiltProject.EndsWith(".app"))
        {
            return pathToBuiltProject;
        }

        if (File.Exists(Path.Combine(pathToBuiltProject, "Contents/Info.plist")))
        {
            return pathToBuiltProject;
        }

        if (!Directory.Exists(pathToBuiltProject))
        {
            return null;
        }

        string[] apps = Directory.GetDirectories(pathToBuiltProject, "*.app", SearchOption.TopDirectoryOnly);
        return apps.Length > 0 ? apps[0] : null;
    }

    private static void ResignAdHoc(string appPath)
    {
        Process process = new Process();
        process.StartInfo.FileName = "/usr/bin/codesign";
        process.StartInfo.Arguments = "--force --deep --sign - \"" + appPath + "\"";
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;
        process.Start();
        process.WaitForExit();
    }

    private static void RemoveKeyFromPlistsInFolder(string folder, string key)
    {
        if (!Directory.Exists(folder))
        {
            return;
        }

        string[] plistPaths = Directory.GetFiles(folder, "*.plist", SearchOption.AllDirectories);
        foreach (string plistPath in plistPaths)
        {
            RemoveKeyFromPlist(plistPath, key);
        }
    }

    private static void RemoveKeyFromPlist(string plistPath, string key)
    {
        if (!File.Exists(plistPath))
        {
            return;
        }

        XmlDocument plist = new XmlDocument();
        plist.PreserveWhitespace = true;
        plist.Load(plistPath);

        XmlNode dictNode = plist.SelectSingleNode("/plist/dict");
        if (dictNode == null)
        {
            return;
        }

        for (int i = 0; i < dictNode.ChildNodes.Count; i++)
        {
            XmlNode node = dictNode.ChildNodes[i];
            if (node.Name != "key" || node.InnerText != key)
            {
                continue;
            }

            XmlNode valueNode = GetNextElementSibling(node);
            dictNode.RemoveChild(node);

            if (valueNode != null)
            {
                dictNode.RemoveChild(valueNode);
            }

            plist.Save(plistPath);
            return;
        }
    }

    private static XmlNode GetNextElementSibling(XmlNode node)
    {
        XmlNode sibling = node.NextSibling;
        while (sibling != null && sibling.NodeType != XmlNodeType.Element)
        {
            sibling = sibling.NextSibling;
        }

        return sibling;
    }
}
