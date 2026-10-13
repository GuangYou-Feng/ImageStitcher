namespace ImageStitcher.Stitching
{
    /// <summary>Feature detector used by the automatic stitcher.</summary>
    public enum StitchAlgorithm
    {
        /// <summary>
        /// SIFT - scale invariant feature transform. Strong matching and high
        /// robustness, the better choice for aerial photography and other
        /// precision-critical work. Slower than ORB.
        /// </summary>
        Sift = 0,

        /// <summary>
        /// ORB - oriented FAST and rotated BRIEF. Much faster and license free,
        /// a good fit when responsiveness matters more than the last bit of accuracy.
        /// </summary>
        Orb = 1
    }
}
