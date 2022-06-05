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
using BepInEx.Logging;
using System.Reflection;

namespace DHHPresetLoader
{
    [BepInPlugin(Guid, Name, Version)]
    [BepInDependency("dhhai4mod", BepInDependency.DependencyFlags.HardDependency)]
    public partial class DHHPresetLoader : BaseUnityPlugin
    {
        public const string Guid = "com.toppac.DHHPresetLoader";
        public const string Version = "0.0.0.3";
        public const string Name = "DHH Preset Loader";
        public static DHHPresetLoader Instance;
        private static Harmony _HarmonyIns;
        internal object dhhRuntimeScript { get; set; }
        internal Action<string> LoadPreset;
        internal Action<string> SavePreset;

        internal void InitMethod()
        {
            var lgs = AccessTools.Method(dhhRuntimeScript.GetType(), "LoadGraphicSetting")
                ?? throw new ArgumentException("TryGetMethod not found");

            var sgs = AccessTools.Method(dhhRuntimeScript.GetType(), "SaveGraphicSetting")
                ?? throw new ArgumentException("TryGetMethod not found");

            Instance.LoadPreset = (path) =>
            {
                if (dhhRuntimeScript != null)
                    lgs.Invoke(dhhRuntimeScript, new object[] { path });
            };

            Instance.SavePreset = (path) =>
            {
                if (dhhRuntimeScript != null)
                    sgs.Invoke(dhhRuntimeScript, new object[] { path });
            };
        }

        internal void LogError(string str)
        {
            Logger.LogError(str);
        }

        private void Init()
        {
            if (Instance) return;
            Instance = this;

            var path = Application.dataPath + "/../DHH_Data";
            var dhhBase = AccessTools.TypeByName("DHH_Base");
            if (!Directory.Exists(path) || dhhBase == null)
            {
                Logger.LogInfo("DHHPresetLoader: Get Type <DHH_Base> failed!");
                Destroy(this);
                return;
            }

            DefDirInfo = new DirectoryInfo(path);

            var dhhMain = AccessTools.TypeByName("DHH_Main");
            var dhhUpdate = AccessTools.Method(dhhMain, "Update");

            _HarmonyIns = new Harmony("DHHPresetLoaderGUI");

            _HarmonyIns.Patch(AccessTools.Method(dhhBase, "CreateRuntime"),
                null, new HarmonyMethod(typeof(Hooks), "GetDHHScript"));

            if (dhhBase.Namespace.ContainsCase("DHH_AI4"))
            {
                _HarmonyIns.Patch(dhhUpdate, null,
                    new HarmonyMethod(typeof(Hooks), "GetAI4MenuOn"));
                return;
            }
            _HarmonyIns.Patch(dhhUpdate, null,
                new HarmonyMethod(typeof(Hooks), "GetMenuOn"));


        }

        private void Awake() => Init();

        private void OnDestroy()
        {
            Hooks.Clear();
            _HarmonyIns?.UnpatchSelf();
            _HarmonyIns = null;
        }

        private void OnGUI()
        {
            if (!Hooks.PanelOn) return;
            DrawWindowGui();
            if (KKAPI.Studio.StudioAPI.InsideStudio)
                DrawFocusGui();
        }

        private void DrawWindowGui()
        {
            // Gui Rect pos report.
            //var golbalSkin = GUI.skin;

            _viewRect = GUILayout.Window(114514, _viewRect, DrawPanelItem, Name);

            if (_viewRect.Contains(new Vector2(Input.mousePosition.x,
                    Screen.height - Input.mousePosition.y)))
            {
                Input.ResetInputAxes();
            }
            //GUI.skin = golbalSkin;
        }

        private void DrawPanelItem(int id)
        {
            GUILayout.BeginVertical();
            {
                // Top Div
                DrawDirTreeView();

                // Buttom Div
                GUILayout.BeginVertical(
                    GUI.skin.box, GUILayout.ExpandWidth(true),
                    GUILayout.ExpandHeight(false));
                {
                    if (GUILayout.Button("Refresh Folder"))
                        UpdateTreeCache();

                    GUILayout.Space(1);
                    if (GUILayout.Button("Open Presets Folder"))
                        Tools.OpenDirInExplorer(_defDirInfo.FullName);

                    GUILayout.Space(1);
                    if (GUILayout.Button("Shell Load Preset"))
                        ShellOpenFile();

                    GUILayout.Space(1);
                    if (GUILayout.Button("Shell Save Preset"))
                        ShellSaveFile();
                }
                GUILayout.EndVertical();
            }
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void DrawDirTreeView()
        {
            ExpandFolder();

            GUILayout.BeginVertical(GUI.skin.box, GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));
            {
                DirViewPort();
            }
            GUILayout.EndVertical();
        }

        private void DirViewPort()
        {
            var emptyKey = string.IsNullOrWhiteSpace(_searchKey);
            if (emptyKey)
            {
                _isSearch = false;
                _searchKey = string.Empty;
            }

            _viewPos = GUILayout.BeginScrollView(
                _viewPos, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            {
                DrawViewItem(DirTree, 0);
            }
            GUILayout.EndScrollView();

            GUILayout.BeginHorizontal();
            {
                GUI.SetNextControlName("DHHPLSI");
                _searchKey = GUILayout.TextField(_searchKey);

                var isSearch = GUILayout.Button(
                    "Search", GUILayout.ExpandWidth(false));

                var isFocus = Event.current.keyCode == KeyCode.Return
                    && GUI.GetNameOfFocusedControl() == "DHHPLSI";

                if (isSearch || isFocus)
                {
                    if (!string.Equals(
                        _searchKey, _tempKey, StringComparison.OrdinalIgnoreCase))
                    {
                        ReSearch();
                        _tempKey = _searchKey;
                    }
                    _isSearch = true;
                }
            }
            GUILayout.EndHorizontal();
        }

        private void ReSearch()
        {
            var files = new List<FileItem>();
            foreach (var i in DirectoryTree.GetAllFiles(DirTree))
            {
                if (i.Name.ContainsCase(_searchKey))
                    files.Add(i);
            }
            _fileItems = files;
        }

        private void DrawViewItem(DirectoryTree dir, int depth)
        {
            var dirFullName = dir.FullName;
            var subDirs = dir.SubDirs;
            var files = dir.Files;
            if (subDirs.Count == 0 && files.Count == 0) return;

            if (!_isSearch)
            {
                var drawFiles = false;

                GUILayout.BeginHorizontal();
                {
                    var defColor = GUI.color;
                    // toggle edge offset;
                    GUILayout.Space(depth * 20f);

                    if (string.Equals(dirFullName, _sePath, StringComparison.OrdinalIgnoreCase))
                    {
                        GUI.color = Color.cyan;
                    }
                    // Toggle comp
                    GUILayout.BeginHorizontal();
                    {
                        if (subDirs.Count > 0 || files.Count > 0)
                        {
                            if (GUILayout.Toggle(_expDirs.Contains(dirFullName),
                                    string.Empty, GUILayout.ExpandWidth(false)))
                            {
                                _expDirs.Add(dirFullName);
                                drawFiles = true;
                            }
                            else { _expDirs.Remove(dirFullName); }
                        }
                        else
                        {
                            GUILayout.Space(20f);
                        }

                        if (NonTranslatedButton(dir.Name, GUI.skin.box,
                                GUILayout.ExpandWidth(true), GUILayout.MinWidth(100)))
                        {
                            if (_sePath.EqualsCase(dirFullName))
                            {
                                if (!_expDirs.Contains(dirFullName))
                                {
                                    _expDirs.Add(dirFullName);
                                }
                                else { _expDirs.Remove(dirFullName); }
                            }
                            _sePath = dirFullName;
                        }
                    }
                    GUILayout.EndHorizontal();
                    GUI.color = defColor;
                }
                GUILayout.EndHorizontal();

                if (_expDirs.Contains(dirFullName))
                {
                    foreach (var subDir in subDirs)
                    {
                        DrawViewItem(subDir, depth + 1);
                    }
                }
                if (drawFiles) DrawFileItems(files, (depth + 3) * 20);
            }
            else if (_fileItems != null)
            {
                foreach (var file in _fileItems)
                {
                    DrawFileItem(file);
                }
            }
        }

        private void DrawFileItems(List<FileItem> files, int offset)
        {
            foreach (var file in files)
            {
                GUILayout.BeginHorizontal();
                {
                    GUILayout.Space(offset);
                    DrawFileItem(file);
                }
                GUILayout.EndHorizontal();
            }
        }

        private void DrawFileItem(FileItem file)
        {
            var defColor = GUI.color;
            GUILayout.BeginHorizontal();
            {
                if (string.Equals(file.FullName, _seFile, StringComparison.OrdinalIgnoreCase))
                {
                    GUI.color = Color.green;
                }

                if (NonTranslatedButton(file.Name, GUI.skin.label,
                        GUILayout.ExpandWidth(true), GUILayout.MinWidth(100)))
                {
                    _seFile = file.FullName;
                    LoadPreset?.Invoke(_seFile);
                }
            }
            GUI.color = defColor;
            GUILayout.EndHorizontal();
        }

        private bool NonTranslatedButton(string text, GUIStyle style, params GUILayoutOption[] options)
        {
            XuaObject?.SendMessage("DisableAutoTranslator");
            try
            {
                return GUILayout.Button(text, style, options);
            }
            finally
            {
                XuaObject?.SendMessage("EnableAutoTranslator");
            }
        }

        private void ShellOpenFile()
        {
            var path = GetShellPath(ShellWindowsControl.DialogType.Load);
            if (string.IsNullOrWhiteSpace(path)) return;
            LoadPreset?.Invoke(path);
        }

        private void ShellSaveFile()
        {
            var path = GetShellPath(ShellWindowsControl.DialogType.Save);
            if (string.IsNullOrWhiteSpace(path)) return;
            SavePreset?.Invoke(path);
        }

        private string GetShellPath(ShellWindowsControl.DialogType dialogType)
        {
            var path = ShellWindowsControl.FileDialog(
                DefExtName, dialogType, DefDirInfo.FullName);
            return path;
        }

#if KKAPI
        public void ShellLoadPreset(string[] pathArr)
        {
            try
            {
                var path = pathArr.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(path)) return;
                LoadPreset(path);
            }
            catch (Exception) { }
        }

        public void ShellSavePreset(string[] pathArr)
        {
            try
            {
                var path = pathArr.FirstOrDefault();
                if (string.IsNullOrWhiteSpace(path)) return;
                SavePreset(path);
            }
            catch (Exception) { }
        }
#endif

        private void ExpandFolder()
        {
            var defPath = _defDirInfo.FullName;
            if (!Directory.Exists(_sePath))
            {
                _sePath = defPath;
            }
            var sePath = _sePath;

            _expDirs.Add(defPath);
            while (!string.IsNullOrEmpty(sePath) && sePath.Length > defPath.Length)
            {
                if (_expDirs.Add(sePath))
                    sePath = Tools.NormalizePath(Path.GetDirectoryName(sePath));
                else
                    break;
            }
        }

        private void UpdateTreeCache()
        {
            if (DirTree != null)
            {
                DirTree.Reset();
                //DirTree.GetCache();
            }
            _fileItems = null;
            _isSearch = false;
            _sePath = string.Empty;
            _seFile = string.Empty;
            _tempKey = string.Empty;
            _searchKey = string.Empty;
        }

        private GameObject XuaObject
        {
            get
            {
                if (_xuaChecked) return _xua;
                _xua = GameObject.Find("___XUnityAutoTranslator");
                _xuaChecked = true;
                return _xua;
            }
        }

        public DirectoryInfo DefDirInfo {
            get => _defDirInfo; set => _defDirInfo = value; }

        //DHH Preset (*.dhs)|*.dhs|All files|*.*
        public static readonly string Filter = "DHHPreset(*.dhs)|*.dhs";
        public static readonly string DefPresetName = "GraphicSetting.dhs";
        public const string DefExtName = "dhs";//.dhs

        private Rect _viewRect = new Rect(0, 0, 260, 460);
        private Vector2 _viewPos;

        private GameObject _xua;
        private bool _xuaChecked;
        private bool _isSearch;

        private string _sePath = string.Empty;
        private string _seFile = string.Empty;
        private string _searchKey = string.Empty;
        private string _tempKey = string.Empty;
        private DirectoryInfo _defDirInfo;

        private HashSet<string> _expDirs = new HashSet<string>();
        private List<FileItem> _fileItems;

        private DirectoryTree _dirTree;
        public DirectoryTree DirTree
        {
            get
            {
                if (_dirTree == null)
                {
                    _dirTree = new DirectoryTree(
                        new DirectoryInfo(DefDirInfo.FullName), $".{DefExtName}");
                }
                return _dirTree;
            }
        }
    }
}
