using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using BepInEx;
using HarmonyLib;
using UnityEngine;
using Studio;

namespace DHHPresetLoader
{
    public partial class DHHPresetLoader : BaseUnityPlugin
    {
        public static Studio.CameraControl.CameraData CamData
        {
            get
            {
                if (_camData == null)
                {
                    try
                    {
                        _camData = Singleton<Studio.Studio>.instance
                            .cameraCtrl.cameraData;
                    }
                    catch { return null; }
                    return _camData;
                }
                return _camData;
            }
        }

        private static Studio.CameraControl.CameraData _camData;

        private bool _miniPanel = true;

        private void DrawFocusGui()
        {
            var rect = _viewRect;
            rect.x += rect.width;
            if (_miniPanel)
            {
                rect.width = 20;
                rect.height = 20;
                if (GUI.Button(rect, ">"))
                {
                    _miniPanel = false;
                }
                return;
            }
            GUILayout.Window(114519, rect, DrawFocusItem, "Focus Set");
        }

        private void DrawFocusItem(int id)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            {
                GUILayout.BeginHorizontal(GUILayout.Height(20));
                {
                    GUILayout.Label(string.Empty);
                    if (GUILayout.Button("<", GUILayout.ExpandWidth(false)))
                    {
                        _miniPanel = true;
                    }
                }
                GUILayout.EndHorizontal();

                DrawCameraData();

                GUILayout.BeginVertical(GUILayout.ExpandWidth(true),
                    GUILayout.ExpandHeight(false));
                {
                    if (GUILayout.Button("Focus select Chara"))
                    {
                        FocusToCharacter();
                    }
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
        }

        private void DrawCameraData()
        {
            GUILayout.BeginVertical(GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            {
                Vector3Item("Position:", CamData.pos);
                Vector3Item("Rotate:", CamData.rotate);
                Vector3Item("Distance:",CamData.distance);
                FloatItem("Camera fieldOfView:", CamData.parse);
            }
            GUILayout.EndVertical();
        }

        private void FocusToCharacter()
        {
            var chara = Tools.GetSelectCharacters().FirstOrDefault();
            if (chara == null) return;

            var bodyBox = Tools.CalculateBounds(chara.charInfo.gameObject);

            var data = CamData;
            if (data == null) return;

            var radius = bodyBox.max.magnitude / 2f;

            var hFov = 2f * Mathf.Atan(
                Mathf.Tan(data.parse * Mathf.Deg2Rad / 2f)
                * Camera.main.aspect) * Mathf.Rad2Deg;

            var fov = Mathf.Min(data.parse, hFov);
            var dist = radius / (Mathf.Sin(fov * Mathf.Deg2Rad / 2f));

            var roate = chara.charInfo.transform.eulerAngles;
            if (bodyBox.max.z > bodyBox.max.x && bodyBox.max.z > bodyBox.max.y)
                roate.y -= 90;

            roate.y -= 180;
            data.rotate = roate;

            data.pos = bodyBox.center;
            data.distance.z = dist * -1 / 2;
            //camera.nearClipPlane = minDistance - maxExtent;
        }

        private void Vector3Item(string label, Vector3 vect)
        {
            GUILayout.Label(label);
            GUILayout.BeginHorizontal();
            {
                FloatItem("X:", vect.x);
                FloatItem("Y:", vect.y);
                FloatItem("Z:", vect.z);
            }
            GUILayout.EndHorizontal();
        }

        private void FloatItem(string label, float fl)
        {
            GUILayout.BeginHorizontal();
            {
                GUILayout.Label(label, GUILayout.MinWidth(20));
                GUILayout.TextField(string.Format(_floatmat, fl),
                    GUILayout.MaxWidth(50));
            }
            GUILayout.EndHorizontal();
        }

        private string _floatmat = "{0:F}";
    }
}
