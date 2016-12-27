/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/11/22
 * Note  : Json格式数据写入
***************************************************************/
using UnityEngine;
using System.Collections;
using System.IO;
using System.Text;

namespace zcode
{
    /// <summary>
    /// SimpleJson格式写入器
    /// </summary>
    public static class SimpleJsonWriter
    {
        /// <summary>
        /// 写入至文件
        /// </summary>
        public static bool WriteToFile(object json, string file_name)
        {
            try
            {
                string text;
                if(WriteToString(json, out text))
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(file_name));
                    File.WriteAllText(file_name, text, Encoding.UTF8);

                    return true;
                }

                return false;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
            }

            return false;
        }

        /// <summary>
        /// 写入至字符串流
        /// </summary>
        public static bool WriteToString(object json, out string str)
        {
            str = null;

            try
            {
                str = SimpleJson.SimpleJson.SerializeObject(json);

                return true;
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex.Message);
            }

            return false;
        }
    }
}