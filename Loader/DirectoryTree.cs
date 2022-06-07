using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Security;
using System.Linq;

namespace DHHPresetLoader
{
    public class FileItem
    {
        public string Name { get; private set; }
        public string FullName { get; private set; }

        public FileItem(FileInfo info)
        {
            Name = info.Name;
            FullName = info.FullName; //PresetLoaderTools.NormalizePath(info.FullName);
        }
    }

    public class DirectoryTree
    {
        private List<DirectoryTree> _subDirs;
        private List<FileItem> _files;

        public DirectoryTree(DirectoryInfo info, string fileExtFilter)
        {
            Info = info;
            Name = Info.Name;
            FileExtFilter = fileExtFilter;
            FullName = Tools.NormalizePath(Info.FullName);
            Reset();
        }

        public DirectoryInfo Info { get; }

        public string Name { get; }
        public string FullName { get; }
        public string FileExtFilter { get; }

        public List<DirectoryTree> SubDirs
        {
            get
            {
                if (_subDirs == null)
                {
                    try
                    {
                        _subDirs = Info.GetDirectories()
                            .OrderBy(x => x.Name, new Tools.ShellStringComparer())
                            .Select(x => new DirectoryTree(x, FileExtFilter))
                            .ToList();
                    }
                    catch (DirectoryNotFoundException) { }
                    catch (SecurityException) { }
                    catch (UnauthorizedAccessException) { }

                    if (_subDirs == null) _subDirs = new List<DirectoryTree>();
                }

                return _subDirs;
            }
        }

        public List<FileItem> Files
        {
            get
            {
                if (_files == null)
                {
                    try
                    {
                        _files = Info.GetFiles()
                            .Where(x => x.Extension.ToLower() == FileExtFilter)
                            .OrderBy(f => f.Name, new Tools.ShellStringComparer())
                            .Select(i => new FileItem(i))
                            .ToList();
                    }
                    catch (FileNotFoundException) { }
                    catch (SecurityException) { }
                    catch (UnauthorizedAccessException) { }

                    if (_files == null) _files = new List<FileItem>();
                }
                return _files;
            }
        }

        public void Reset()
        {
            _subDirs = null;
            _files = null;
        }

        public void GetCache()
        {
            var subDirs = SubDirs;
            _ = Files;
            CacheChildren(subDirs);
        }

        private void CacheChildren(List<DirectoryTree> treeList)
        {
            foreach (var dir in treeList)
            {
                var subTree = dir.SubDirs;
                _ = dir.Files;
                CacheChildren(subTree);
            }
        }

        public static List<FileItem> GetAllFiles(DirectoryTree dirTree)
        {
            var Result = new List<FileItem>();
            GetChildrenFiles(dirTree, Result);
            return Result;
        }

        private static void GetChildrenFiles(
            DirectoryTree dirTree, List<FileItem> fileItems)
        {
            fileItems.AddRange(dirTree.Files);
            foreach (var dir in dirTree.SubDirs)
            {
                GetChildrenFiles(dir, fileItems);
            }
        }
    }
}
