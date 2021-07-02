using System.Collections.Generic;

namespace AC.Strcuts
{
    /// <summary>
    /// 命令序列
    /// </summary>
    [System.Serializable]
    public class CommandSequence
    {
        /// <summary>
        /// 名称.
        /// </summary>
        public string name;

        /// <summary>
        /// 命令序列.
        /// </summary>
        public List<Command> commands = new List<Command>();
    }
}