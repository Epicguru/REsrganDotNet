using System.Text;

namespace RealESRGAN;

/// <summary>
/// A collection of input arguments to be used in the <see cref="UpscaleProcess"/> class.
/// It has a <see cref="Clone"/> method for your convenience.
/// </summary>
[Serializable]
public class UpscaleInputArgs
{
    /// <summary>
    /// The input file or folder path.
    /// </summary>
    public string InputPath { get; set; }
    /// <summary>
    /// The output file or folder path.
    /// </summary>
    public string OutputPath { get; set; }
    /// <summary>
    /// The path of the resrgan executable file.
    /// </summary>
    public string ExecutablePath { get; set; }
    /// <summary>
    /// A list of GPU-specific settings.
    /// Also allows for specific GPU(s) to be used rather than letting resrgan choose a GPU automatically.
    /// </summary>
    public List<GPUInfo> GPUs { get; } = new List<GPUInfo>();
    /// <summary>
    /// The scaling factor. Note: 
    /// </summary>
    public int Scale { get; set; } = 4;
    /// <summary>
    /// The relative path to the folder that contains the built upscaler models.
    /// Defaults to 'models'.
    /// </summary>
    public string ModelDirectoryPath { get; set; } = "models";
    /// <summary>
    /// The name of the upscaler model to use. Defaults to realesr-animevideov3.
    /// </summary>
    public string ModelName { get; set; } = "realesr-animevideov3";
    /// <summary>
    /// Should TTA mode be used?
    /// </summary>
    public bool EnableTTAMode { get; set; }
    /// <summary>
    /// The format of the upscaled image.
    /// </summary>
    public ImageFormat OutputFormat { get; set; } = ImageFormat.Default;

    public UpscaleInputArgs(string executablePath, string inputPath, string outputPath)
    {
        ExecutablePath = executablePath;
        InputPath = inputPath;
        OutputPath = outputPath;
    }

    public string BuildArgs()
    {
        var str = new StringBuilder(256);

        void WrapQuote(string input)
        {
            str.Append('"');
            str.Append(input);
            str.Append('"');
        }

        void Compound(char op, Func<GPUInfo, string> extract)
        {
            if (GPUs.Count == 0)
                return;

            str.Append(" -");
            str.Append(op);
            str.Append(' ');

            for (int i = 0; i < GPUs.Count; i++)
            {
                var gpu = GPUs[i];
                str.Append(extract(gpu));
                if (i < GPUs.Count - 1)
                    str.Append(',');
            }
        }

        // Input
        str.Append(" -i ");
        WrapQuote(InputPath);

        // Output
        str.Append(" -o ");
        WrapQuote(OutputPath);

        // Scale
        if (Scale is > 1 and < 4)
            str.Append(" -s ").Append(Scale);

        if (ModelDirectoryPath != "models")
        {
            str.Append(" -m ");
            WrapQuote(ModelDirectoryPath);
        }

        // Model name
        str.Append(" -n ").Append(ModelName);

        // Output format
        if (OutputFormat != ImageFormat.Default)
            str.Append(" -f ").Append(OutputFormat.ToString().ToLowerInvariant());

        // TTA mode
        if (EnableTTAMode)
            str.Append(" -x");

        // GPU id.
        Compound('g', g => g.ID.ToString());

        // Tile size:
        Compound('t', g => g.TileSize.ToString());

        // Thread count:
        Compound('j', g => $"{g.LoadThreadCount}:{g.ProcessThreadCount}:{g.SaveThreadCount}");

        return str.ToString();
    }

    /// <summary>
    /// Creates a deep clone of this object.
    /// </summary>
    public UpscaleInputArgs Clone()
    {
        var clone = new UpscaleInputArgs(ExecutablePath, InputPath, OutputPath)
        {
            EnableTTAMode = EnableTTAMode,
            Scale = Scale,
            OutputFormat = OutputFormat,
            ModelDirectoryPath = ModelDirectoryPath,
            ModelName = ModelName,
        };

        clone.GPUs.AddRange(GPUs.Select(g => g.Clone()));

        return clone;
    }
}
