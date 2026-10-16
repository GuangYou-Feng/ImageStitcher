namespace ImageStitcher.Stitching
{
    /// <summary>自动拼接使用的特征检测算法。</summary>
    public enum StitchAlgorithm
    {
        /// <summary>
        /// SIFT —— 尺度不变特征变换。匹配能力强、鲁棒性高，适合航拍等对精度要求
        /// 较高的场景。速度比 ORB 慢。
        /// </summary>
        Sift = 0,

        /// <summary>
        /// ORB —— 带方向的 FAST 关键点与旋转 BRIEF 描述子。速度更快且无授权限制，
        /// 在响应速度比极致精度更重要时更为合适。
        /// </summary>
        Orb = 1
    }
}
