namespace AC.Strcuts
{
    /// <summary>
    /// 命令类型.
    /// </summary>
    public enum CommandType
    {
        /// <summary>
        /// 指定对象播放动画.
        /// </summary>
        PlayAnimation,

        /// <summary>
        /// 停用指定对象的所有子对象.
        /// </summary>
        DeactivateChildren,

        /// <summary>
        /// 指定对象的所有子对象播放动画.
        /// </summary>
        PlayChildAnimation,

        /// <summary>
        /// 等待指定时长.
        /// </summary>
        Wait,

        /// <summary>
        /// 等待指定对象播放完成.
        /// </summary>
        WaitAnimation,

        /// <summary>
        /// 等待指定对象的所有子对象播放完成.
        /// </summary>
        WaitChildAnimation,

        /// <summary>
        /// 停用指定对象
        /// </summary>
        Deactivate,
    }
}