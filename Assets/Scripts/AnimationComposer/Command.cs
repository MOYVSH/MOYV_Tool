using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AC.Strcuts
{
    /// <summary>
    /// 动画命令.
    /// </summary>
    [System.Serializable]
    public class Command
    {
        /// <summary>
        /// 命令类型
        /// </summary>
        public CommandType type;

        /// <summary>
        /// 命令目标对象.
        /// </summary>
        public GameObject target;

        /// <summary>
        /// 动画名称.
        /// </summary>
        public string animName;

        /// <summary>
        /// 动画时长.
        /// </summary>
        public float animTime;

        /// <summary>
        /// 间隔时间.
        /// </summary>
        public float time;

        /// <summary>
        /// 反向遍历所有子对象.
        /// </summary>
        public bool reverse;

        /// <summary>
        /// 反播动画
        /// </summary>
        public bool reversePlay;
    }
}