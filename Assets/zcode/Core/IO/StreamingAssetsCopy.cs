/***************************************************************
* Author: Zhang Minglin
* Note  : Application.streamingAssetsPath目录下拷贝
***************************************************************/
using System.Collections;
using UnityEngine;

namespace zcode
{
    /// <summary>
    /// Application.streamingAssetsPath目录下拷贝
    /// </summary>
    public class StreamingAssetsCopy
    {
        /// <summary>
        /// 是否结束
        /// </summary>
        public bool isDone { get; private set; }

        /// <summary>
        /// 拷贝结果
        /// </summary>
        public emIOOperateCode resultCode { get; private set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string error { get; private set; }

        /// <summary>
        ///   从Application.streamingAssetsPath目录下拷贝
        /// </summary>
        public IEnumerator Copy(string src, string dest)
        {
#if UNITY_EDITOR || UNITY_STANDALONE_WIN || UNITY_IPHONE
            src = "file:///" + src;
#endif
            SetResult(false, emIOOperateCode.Succeed, null);
            do
            {
                using (WWW w = new WWW(src))
                {
                    yield return w;

                    if (!string.IsNullOrEmpty(w.error))
                    {
                        SetResult(true, emIOOperateCode.Fail, w.error);
                    }
                    else
                    {
                        if (w.isDone && w.bytes.Length > 0)
                        {
                            var ret = zcode.FileHelper.WriteBytesToFile(dest, w.bytes, w.bytes.Length);
                            SetResult(true, ret, null);
                        }
                    }
                }
            } while (!isDone);
        }

        /// <summary>
        /// 
        /// </summary>
        void SetResult(bool isDone, emIOOperateCode result, string error)
        {
            this.isDone = isDone;
            this.resultCode = result;
            this.error = error;
        }
    }
}