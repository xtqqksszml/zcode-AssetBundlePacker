/***************************************************************
 * Copyright 2016 By Zhang Minglin
 * Author: Zhang Minglin
 * Create: 2016/11/29
 * Note  : 泛型交换函数
***************************************************************/
using UnityEngine;
using System.Collections;

namespace zcode
{
    public partial class Utility
    {
        /// <summary>
        /// 泛型交换函数
        /// </summary>
        public static void Swap<T>(ref T p1, ref T p2)
        {
            T temp = p1;
            p1 = p2;
            p2 = temp;
        }
    }
}
