/***************************************************************
* Author: Zhang Minglin
* Note  : IO错误代码定义
***************************************************************/

namespace zcode
{
    /// <summary>
    /// IO操作代码
    /// </summary>
    public enum emIOOperateCode
    {
        Succeed = 0,                // 成功
        Fail = 1,                   // 失败
        DiskFull = 2,               // 存储空间已满
    }
}

