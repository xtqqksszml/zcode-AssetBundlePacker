/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/01/20
 * Note  : GUI辅助绘制函数
***************************************************************/
using UnityEngine;
using System.Collections;
using UnityEditor;

namespace zcode.AssetBundlePacker
{
    public class GUILayoutHelper
    {
        /// <summary>
        /// Draw a distinctly different looking header label
        /// </summary>
        static public bool DrawHeader(string text)
        { return DrawHeader(text, text, false, false); }

        /// <summary>
        /// Draw a distinctly different looking header label
        /// </summary>
        static public bool DrawHeader(string text, string key)
        { return DrawHeader(text, key, false, false); }

        /// <summary>
        /// Draw a distinctly different looking header label
        /// </summary>
        static public bool DrawHeader(string text, bool detailed) 
        { return DrawHeader(text, text, detailed, !detailed); }

        /// <summary>
        /// Draw a distinctly different looking header label
        /// </summary>
        static public bool DrawHeader(string text, string key, bool forceOn, bool minimalistic)
        {
            bool state = EditorPrefs.GetBool(key, true);

            if (!minimalistic) GUILayout.Space(3f);
            if (!forceOn && !state) GUI.backgroundColor = new Color(0.8f, 0.8f, 0.8f);
            GUILayout.BeginHorizontal();
            GUI.changed = false;

            if (minimalistic)
            {
                if (state) text = "\u25BC" + (char)0x200a + text;
                else text = "\u25BA" + (char)0x200a + text;

                GUILayout.BeginHorizontal();
                GUI.contentColor = EditorGUIUtility.isProSkin ? new Color(1f, 1f, 1f, 0.7f) : new Color(0f, 0f, 0f, 0.7f);
                if (!GUILayout.Toggle(true, text, "PreToolbar2", GUILayout.MinWidth(20f))) state = !state;
                GUI.contentColor = Color.white;
                GUILayout.EndHorizontal();
            }
            else
            {
                text = "<b><size=11>" + text + "</size></b>";
                if (state) text = "\u25BC " + text;
                else text = "\u25BA " + text;
                if (!GUILayout.Toggle(true, text, "dragtab", GUILayout.MinWidth(20f))) state = !state;
            }

            if (GUI.changed) EditorPrefs.SetBool(key, state);

            if (!minimalistic) GUILayout.Space(2f);
            GUILayout.EndHorizontal();
            GUI.backgroundColor = Color.white;
            if (!forceOn && !state) GUILayout.Space(3f);
            return state;
        }

        /// <summary>
        /// 
        /// </summary>
        static public bool Toggle(bool value, string text, bool disabled, params GUILayoutOption[] options)
        {
            if (disabled) { EditorGUI.BeginDisabledGroup(true); }
            bool result = GUILayout.Toggle(value, text, options);
            if (disabled) { EditorGUI.EndDisabledGroup(); }

            return result;
        }
    }
}