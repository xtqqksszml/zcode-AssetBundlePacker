/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/12/20
 * Note  : 仅适用于GUI自动布局的多选组， 适用如编辑器窗口绘制
 *         1. 支持Ctrl与Shift多选控制
 *         2. 支持自定义渲染方式
 *         3. 数据组织方式自定义(手动实现Node,NodeGroup类)
***************************************************************/
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// 
/// </summary>
public class GUILayoutMultiSelectGroup
{
    /// <summary>
    /// 节点
    /// </summary>
    public abstract class Node
    {
        /// <summary>
        /// 索引
        /// </summary>
        public int Index;

        /// <summary>
        /// 是否选中
        /// </summary>
        public bool IsSelect;

        /// <summary>
        /// 渲染
        /// </summary>
        public abstract OperateResult Draw();
    }

    /// <summary>
    /// 节点组
    /// </summary>
    public abstract class NodeGroup
    {
        /// <summary>
        /// 渲染
        /// </summary>
        public abstract OperateResult Draw();

        /// <summary>
        /// 根据索引区间选中指定数量的节点
        /// </summary>
        public abstract List<Node> GetRange(int begin, int end);
    }

    /// <summary>
    /// 操作结果
    /// </summary>
    public class OperateResult
    {
        /// <summary>
        /// 选中的节点
        /// </summary>
        public Node SelectNode;

        /// <summary>
        /// 状态（可由子类赋值）
        /// </summary>
        public object Status;
    }

    /// <summary>
    /// 
    /// </summary>
    public NodeGroup Group;

    /// <summary>
    /// 
    /// </summary>
    public List<Node> SelectNodes = new List<Node>();

    /// <summary>
    /// 
    /// </summary>
    Node last_click_node_;

    /// <summary>
    ///   
    /// </summary>
    private Vector2 scroll_ = Vector2.zero;

    public GUILayoutMultiSelectGroup(NodeGroup group)
    {
        Group = group;
    }

    /// <summary>
    /// 更新选中操作
    /// </summary>
    void UpdateSelectNodeOperate(Node select)
    {
        if (select != null)
        {
            bool is_ctrl_click = Event.current.control;
            bool is_shift_click = Event.current.shift;

            //选中操作
            if (is_ctrl_click)
            {
                ToggleSelectNode(select);
                if (last_click_node_ == null || select.Index < last_click_node_.Index)
                    last_click_node_ = select;
            }
            else if (is_shift_click)
            {
                ClearSelectedNodes();
                
                if(Group != null)
                {
                    int begin = 0;
                    int end = 0;
                    if (last_click_node_.Index < select.Index)
                    {
                        begin = last_click_node_.Index;
                        end = select.Index;
                    }
                    else
                    {
                        begin = select.Index;
                        end = last_click_node_.Index;
                    }
                    var list = Group.GetRange(begin, end);
                    SelectMultiNode(list);
                }
            }
            else
            {
                //消除之前选中的数据的焦点
                ClearSelectedNodes();
                SelectNode(select);
                last_click_node_ = select;
            }
        }
    }

    /// <summary>
    /// 
    /// </summary>
    void SelectMultiNode(List<Node> nodes)
    {
        if (nodes == null || nodes.Count == 0)
            return;

        for (int i = 0; i < nodes.Count; ++i)
            SelectNode(nodes[i]);
    }

    /// <summary>
    /// 
    /// </summary>
    void SelectNode(Node node)
    {
        if (node != null && !SelectNodes.Contains(node))
        {
            node.IsSelect = true;
            SelectNodes.Add(node);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    bool ToggleSelectNode(Node node)
    {
        if (node == null)
            return false;

        if (SelectNodes.Contains(node))
        {
            node.IsSelect = false;
            SelectNodes.Remove(node);
        }
        else
        {
            node.IsSelect = true;
            SelectNodes.Add(node);
        }

        return node.IsSelect;
    }

    /// <summary>
    /// 
    /// </summary>
    void ClearSelectedNodes()
    {
        for (int i = 0; i < SelectNodes.Count; i++)
        {
            SelectNodes[i].IsSelect = false;
        }
        SelectNodes.Clear();
    }

    /// <summary>
    /// 
    /// </summary>
    public OperateResult Draw(bool alwaysShowHorizontal = false, bool alwaysShowVertical = false)
    {
        scroll_ = GUILayout.BeginScrollView(scroll_, alwaysShowHorizontal, alwaysShowVertical);
        OperateResult result = null;
        if (Group != null)
            result = Group.Draw();
        GUILayout.EndScrollView();
       
        if (result != null)
            UpdateSelectNodeOperate(result.SelectNode);
        return result;
    }
}
