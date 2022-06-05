using Studio;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;

namespace DHHPresetLoader
{
    public static class Tools
    {
        public static IEnumerable<OCIChar> GetSelectCharacters()
        {
            return GuideObjectManager.Instance.selectObjectKey
                .Select(x => Studio.Studio.GetCtrlInfo(x) as OCIChar)
                .Where(x => x != null);
        }

        public static Bounds CalculateBounds(GameObject go)
        {
            var b = new Bounds(go.transform.position, Vector3.zero);
            UnityEngine.Object[] rList = go.GetComponentsInChildren(typeof(Renderer));
            foreach (Renderer r in rList)
            {
                b.Encapsulate(r.bounds);
            }
            return b;
        }    

        public static string NormalizePath(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return Path.GetFullPath(value).Replace('\\', '/').TrimEnd('/').ToLower() + "/";
        }

        public class ShellStringComparer : IComparer<string>
        {
            [DllImport("Shlwapi.dll", CharSet = CharSet.Unicode)]
            private static extern int StrCmpLogicalW(string x, string y);

            public int Compare(string x, string y)
            {
                return StrCmpLogicalW(x, y);
            }
        }

        public static void OpenDirInExplorer(string path)
        {
            try
            {
                Process.Start("explorer.exe", $"\"{Path.GetFullPath(path)}\"");
            }
            catch (Exception) { }
        }

#if FULLDEC
        public static float CalculateDist(Camera c, GameObject go, out float radius)
        {
            var b = CalculateBounds(go);
            var max = b.size;

            radius = max.magnitude / 2f;
            var horizontalFOV = 2f * Mathf.Atan(Mathf.Tan(c.fieldOfView * Mathf.Deg2Rad / 2f) * c.aspect) * Mathf.Rad2Deg;

            var fov = Mathf.Min(c.fieldOfView, horizontalFOV);
            return radius / (Mathf.Sin(fov * Mathf.Deg2Rad / 2f));
        }

        public static void FocusCameraOnGameObject(Camera c, GameObject go)
        {
            var dist = CalculateDist(c, go, out var radius);

            c.transform.SetPositionAndRotation(go.transform.position, 
                go.transform.rotation);
            var pos = c.transform.forward * -dist;
            c.transform.position += pos;

            if (c.orthographic) c.orthographicSize = radius;
            //c.transform.LookAt(b.center);
        }
#endif
    }
}