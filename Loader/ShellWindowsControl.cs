using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace DHHPresetLoader
{
    public class ShellWindowsControl
    {
        public static string FileDialog(
            string extend, DialogType dialogType, string dialogPath)
        {
            OpenFileName ofn = new OpenFileName();

            ofn.structSize = Marshal.SizeOf(ofn);
            ofn.filter = $"{extend}(*.{extend})\0*.{extend}\0\0";
            ofn.file = new string(new char[260]);
            ofn.maxFile = ofn.file.Length;
            ofn.fileTitle = new string(new char[64]);
            ofn.maxFileTitle = ofn.fileTitle.Length;
            ofn.initialDir = dialogPath;//UnityEngine.Application.dataPath;
            ofn.title = "Process: " + extend + " file";
            ofn.defExt = extend;
            //OFN_EXPLORER|OFN_FILEMUSTEXIST|OFN_PATHMUSTEXIST| OFN_ALLOWMULTISELECT|OFN_NOCHANGEDIR
            ofn.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000200 | 0x00000008;

            bool isPath;
            if (dialogType == DialogType.Load)
                isPath = ShowOpen(ofn);
            else 
                isPath = ShowSave(ofn);
            if (!isPath) 
                return null;
            return ofn.file;
        }

        public static bool ShowOpen([In, Out] OpenFileName ofn)
        {
            ofn.dlgOwner = GetForegroundWindow();
            return GetOpenFileName(ofn);
        }

        public static bool ShowSave([In, Out] OpenFileName ofn)
        {
            ofn.dlgOwner = GetForegroundWindow();
            return GetSaveFileName(ofn);
        }

        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Unicode)]
        public static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

        [DllImport("Comdlg32.dll", SetLastError = true, ThrowOnUnmappableChar = true, CharSet = CharSet.Unicode)]
        public static extern bool GetSaveFileName([In, Out] OpenFileName ofn);

        [DllImport("user32.dll", CharSet = CharSet.Unicode, ExactSpelling = true)]
        public static extern IntPtr GetForegroundWindow();

        public enum DialogType
        {
            Load,
            Save
        }
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public class OpenFileName
    {
        public int structSize = 0;
        public IntPtr dlgOwner = IntPtr.Zero;
        public IntPtr instance = IntPtr.Zero;
        public String filter = null;
        public String customFilter = null;
        public int maxCustFilter = 0;
        public int filterIndex = 0;
        public String file = null;
        public int maxFile = 0;
        public String fileTitle = null;
        public int maxFileTitle = 0;
        public String initialDir = null;
        public String title = null;
        public int flags = 0;
        public short fileOffset = 0;
        public short fileExtension = 0;
        public String defExt = null;
        public IntPtr custData = IntPtr.Zero;
        public IntPtr hook = IntPtr.Zero;
        public String templateName = null;
        public IntPtr reservedPtr = IntPtr.Zero;
        public int reservedInt = 0;
        public int flagsEx = 0;
    }
}
