/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2015/12/17
 * Note  : 文件常用操作
***************************************************************/
using UnityEngine;
using System.Collections;
using System.IO;
using System.Collections.Generic;
using System;

namespace zcode
{
    /// <summary>
    /// 
    /// </summary>
    public static class FileHelper
    {
        /// <summary>
        ///   拷贝文件
        /// </summary>
        public static bool CopyFile(string src, string dest, bool overwrite = false)
        {
            //不存在则返回
            if (!File.Exists(src))
                return false;

            //保证路径存在
            string directory = Path.GetDirectoryName(dest);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            File.Copy(src, dest, overwrite);

            return true;
        }

        /// <summary>
        /// 拷贝原目录下所有文件和文件夹至目标目录
        /// </summary>
        public static bool CopyDirectoryAllChildren(string scr_folder, string dest_folder)
        {
            try
            {
                if (!Directory.Exists(dest_folder))
                {
                    Directory.CreateDirectory(dest_folder);
                    File.SetAttributes(dest_folder, File.GetAttributes(scr_folder));
                }

                if (dest_folder[dest_folder.Length - 1] != Path.DirectorySeparatorChar)
                    dest_folder = dest_folder + Path.DirectorySeparatorChar;

                string[] files = Directory.GetFiles(scr_folder);
                foreach (string file in files)
                {
                    if (File.Exists(dest_folder + Path.GetFileName(file)))
                        continue;
                    File.Copy(file, dest_folder + Path.GetFileName(file), true);
                }

                string[] dirs = Directory.GetDirectories(scr_folder);
                foreach (string dir in dirs)
                {
                    CopyDirectoryAllChildren(dir, dest_folder + Path.GetFileName(dir));
                }
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }

            return true;
        }

        /// <summary>
        /// 写入文件
        /// </summary>
        /// <param name="path">文件全局路径</param>
        /// <param name="text">写入的内容.</param>
        public static void WriteTextToFile(string path, string text)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(text);
            WriteBytesToFile(path, bytes, bytes.Length);
        }

        /// <summary>
        /// 写入文件
        /// </summary>
        /// <param name="path">文件全局路径</param>
        /// <param name="bytes">写入的内容.</param>
        /// <param name="length">写入长度.</param>
        public static void WriteBytesToFile(string path, byte[] bytes, int length)
        {
            //创建文件夹
            Directory.CreateDirectory(Path.GetDirectoryName(path));

            FileInfo t = new FileInfo(path);
            using (Stream sw = t.Open(FileMode.Create, FileAccess.ReadWrite))
            {
                if (bytes != null && length > 0)
                {
                    //以行的形式写入信息
                    sw.Write(bytes, 0, length);
                }
            }
        }

        /// <summary>
        /// 获取文件下所有文件大小
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static int GetAllFileSize(string filePath)
        {
            int sum = 0;
            if (!Directory.Exists(filePath))
            {
                return 0;
            }

            DirectoryInfo dti = new DirectoryInfo(filePath);

            FileInfo[] fi = dti.GetFiles();

            for (int i = 0; i < fi.Length; ++i )
            {
                sum += Convert.ToInt32(fi[i].Length / 1024);
            }

            DirectoryInfo[] di = dti.GetDirectories();

            if (di.Length > 0)
            {
                for (int i = 0; i < di.Length; i++)
                {
                    sum += GetAllFileSize(di[i].FullName);
                }
            }
            return sum;
        }

        /// <summary>
        /// 获取指定文件大小
        /// </summary>
        /// <param name="file_path"></param>
        /// <returns></returns>
        public static long GetFileSize(string file_path)
        {
            long sum = 0;
            if (!File.Exists(file_path))
            {
                return 0;
            }
            else
            {
                FileInfo Files = new FileInfo(file_path);
                sum += Files.Length;
            }
            return sum;
        }

        /// <summary>
        ///   创建本地AssetBundle文件
        /// </summary>
        /// <param name="path">文件全局路径</param>
        /// <param name="bytes">写入的内容.</param>
        /// <param name="length">写入长度.</param>
        static void CreateAssetbundleFile(string path, byte[] bytes, int length)
        {
            FileInfo t = new FileInfo(path);
            using (Stream sw = t.Open(FileMode.Create, FileAccess.ReadWrite))
            {
                if (bytes != null && length > 0)
                {
                    //以行的形式写入信息
                    sw.Write(bytes, 0, length);
                }
            }
        }

        /// <summary>
        ///   读取本地AssetBundle文件
        /// </summary>
        static IEnumerator LoadAssetbundleFromLocal(string path, string name)
        {
            WWW w = new WWW("file:///" + path + "/" + name);

            yield return w;

            if (w.isDone)
            {
                GameObject.Instantiate(w.assetBundle.mainAsset);
            }
        }

        /// <summary>
        ///   
        /// </summary>
        public static IEnumerator CopyStreamingAssetsToFile(string src, string dest)
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IPHONE
            src = "file:///" + src;
#endif
            using (WWW w = new WWW(src))
            {
                yield return w;

                if (string.IsNullOrEmpty(w.error))
                {
                    while (w.isDone == false)
                        yield return null;

                    //保证路径存在
                    string directory = Path.GetDirectoryName(dest);
                    if (!Directory.Exists(directory))
                        Directory.CreateDirectory(directory);

                    zcode.FileHelper.WriteBytesToFile(dest, w.bytes, w.bytes.Length);
                }
                else
                {
                    Debug.LogWarning(w.error);
                }
            }
        }

        /// <summary>
        /// 删除文件.
        /// </summary>
        /// <param name="path">删除完整文件夹路径.</param>
        /// <param name="name">删除文件的名称.</param>
        public static void DeleteFile(string path, string name)
        {
            File.Delete(path + name);
        }
        /// <summary>
        /// 删除文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="filesName"></param>
        /// <returns></returns>
        public static bool DeleteFiles(string path, string filesName)
        {
            bool isDelete = false;
            try
            {
                if (Directory.Exists(path))
                {
                    if (File.Exists(path + "\\" + filesName))
                    {
                        File.Delete(path + "\\" + filesName);
                        isDelete = true;
                    }
                }
            }
            catch
            {
                return isDelete;
            }
            return isDelete;
        }

        /// <summary>
        ///   删除文件夹下所有子文件夹与文件
        /// </summary>
        public static void DeleteAllChild(string path, FileAttributes filter)
        {
            if (!Directory.Exists(path))
                return;

            DirectoryInfo dir = new DirectoryInfo(path);
            FileInfo[] files = dir.GetFiles("*");
            for(int i = 0 ; i < files.Length ; ++i)
            {
                if ((files[i].Attributes & filter) > 0)
                    continue;
                if (File.Exists(files[i].FullName))
                    File.Delete(files[i].FullName);
            }
            DirectoryInfo[] dirs = dir.GetDirectories("*");
            for (int i = 0; i < dirs.Length; ++i)
            {
                if ((dirs[i].Attributes & filter) > 0)
                    continue;

                if (Directory.Exists(dirs[i].FullName))
                    Directory.Delete(dirs[i].FullName, true);
            }
        }

        /// <summary>
        ///   绝对路径转相对路径
        /// </summary>
        public static string AbsoluteToRelativePath(string root_path, string absolute_path)
        {
            absolute_path = absolute_path.Replace('\\', '/');
            int last_idx = absolute_path.LastIndexOf(root_path);
            if (last_idx < 0)
                last_idx = absolute_path.ToLower().LastIndexOf(root_path.ToLower());
            if (last_idx < 0)
                return absolute_path;

            int start = last_idx + root_path.Length;
            int length = absolute_path.Length - start;
            return absolute_path.Substring(start, length);
        }

        /// <summary>
        ///   获得取除路径扩展名的路径
        /// </summary>
        public static string GetPathWithoutExtension(string full_name)
        {
            int last_idx = full_name.LastIndexOfAny(".".ToCharArray());
            if (last_idx < 0)
                return full_name;

            return full_name.Substring(0, last_idx);
        }
    }
}