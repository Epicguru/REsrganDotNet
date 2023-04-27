namespace RealESRGAN;

/// <summary>
/// Specifies settings that a specific GPU should use.
/// </summary>
[Serializable]
public class GPUInfo
{
    /// <summary>
    /// The ID of the GPU. This is a zero-based index.
    /// </summary>
    public int ID { get; set; }
    /// <summary>
    /// The tile size to use when upscaling.
    /// Must be exactly zero or greater than 31.
    /// Set to zero or to let resrgan choose automatically.
    /// </summary>
    public int TileSize { get; set; } = 0;
    /// <summary>
    /// The number of threads to use when decoding input images.
    /// Defaults to 1.
    /// </summary>
    public int LoadThreadCount { get; set; } = 1;
    /// <summary>
    /// The number of threads to use when processing images.
    /// Defaults to 2.
    /// </summary>
    public int ProcessThreadCount { get; set; } = 2;
    /// <summary>
    /// The number of threads to use when encoding the upscaled images.
    /// Defaults to 2.
    /// </summary>
    public int SaveThreadCount { get; set; } = 2;

    public GPUInfo(int id)
    {
        ID = id;
    }

    public GPUInfo Clone() => new GPUInfo(ID)
    {
        TileSize = TileSize,
        LoadThreadCount = LoadThreadCount,
        ProcessThreadCount = ProcessThreadCount,
        SaveThreadCount = SaveThreadCount,
    };
}
