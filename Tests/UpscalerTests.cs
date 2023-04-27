using FluentAssertions;
using RealESRGAN;
using Xunit.Abstractions;


namespace Tests;

public class UpscalerTests
{
    private const string EXEC = "./windows/realesrgan-ncnn-vulkan";

    private readonly ITestOutputHelper output;

    public UpscalerTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Theory]
    [InlineData("./does_not_exist")]
    [InlineData("./sub/folder/")]
    public void MissingDirectory(string inputPath)
    {
        output.WriteLine("Working dir: " + new DirectoryInfo("./").FullName);
        Assert.Fail($"Why no work");
        Assert.ThrowsAsync<FileNotFoundException>(() => UpscaleProcess.Run(new UpscaleInputArgs(EXEC, inputPath, "./output")));
    }

    [Fact]
    public async Task IsInstantlyCancelled()
    {
        using var cancelTrigger = new CancellationTokenSource();
        cancelTrigger.Cancel();

        var result = await UpscaleProcess.Run(new UpscaleInputArgs(EXEC, "./input", "./output"), cancellationToken: cancelTrigger.Token);
        Assert.NotEqual(0, result.ExitCode);
    }

    [Fact]
    public async Task UpscaleSingle()
    {
        const string INPUT_PATH = "./samples/single_4k.jpg";
        const string OUTPUT_PATH = "./single_upscaled.jpg";
        if (File.Exists(OUTPUT_PATH))
            File.Delete(OUTPUT_PATH);

        var progresses = new List<double>(2048);

        var result = await UpscaleProcess.Run(new UpscaleInputArgs(EXEC, INPUT_PATH, OUTPUT_PATH)
        {
            ExecutablePath = "./windows/realesrgan-ncnn-vulkan"
        }, progresses.Add);

        Assert.Equal(0, result.ExitCode);
        Assert.True(File.Exists(OUTPUT_PATH));
        Assert.NotEmpty(progresses);

        double last = -1;
        foreach (var p in progresses)
        {
            Assert.True(p >= 0.0);
            Assert.True(p <= 1.0);
            Assert.True(p >= last);
        }

        output.WriteLine($"Upscaling 4k image produced {progresses.Count} percentages.");

        long baseSize = new FileInfo(INPUT_PATH).Length;
        long upscaledSize = new FileInfo(OUTPUT_PATH).Length;
        Assert.True(upscaledSize > baseSize);
    }

    [Fact]
    public async Task UpscaleSingle_Cancel()
    {
        const string INPUT_PATH = "./samples/single_4k.jpg";
        const string OUTPUT_PATH = "./single_upscaled.jpg";
        if (File.Exists(OUTPUT_PATH))
            File.Delete(OUTPUT_PATH);

        using var src = new CancellationTokenSource();

        var result = await UpscaleProcess.Run(new UpscaleInputArgs(EXEC, INPUT_PATH, OUTPUT_PATH)
        {
            ExecutablePath = "./windows/realesrgan-ncnn-vulkan"
        }, p =>
        {
            if (p > 0)
            {
                output.WriteLine($"Got progress: {p:F3}, cancelling...");
                src.Cancel();
            }
        },
        src.Token);

        Assert.NotEqual(0, result.ExitCode);
        output.WriteLine($"Exit code: {result.ExitCode}, {result.ErrorMessage}");
    }

    [Fact]
    public async Task UpscaleMultiple()
    {
        const string INPUT_PATH = "./samples/folder";
        const string OUTPUT_PATH = "./multiple";
        if (Directory.Exists(OUTPUT_PATH))
            Directory.Delete(OUTPUT_PATH, true);

        var progresses = new List<double>(2048);

        var args = new UpscaleInputArgs(EXEC, INPUT_PATH, OUTPUT_PATH)
        {
            ExecutablePath = "./windows/realesrgan-ncnn-vulkan",
        };
        var result = await UpscaleProcess.Run(args, progresses.Add);

        output.WriteLine(args.BuildArgs());

        result.ExitCode.Should().Be(0);
        Directory.Exists(OUTPUT_PATH).Should().BeTrue();
        progresses.Should().NotBeEmpty();

        var srcFiles = new DirectoryInfo(INPUT_PATH).GetFiles();
        var dstFiles = new DirectoryInfo(OUTPUT_PATH).GetFiles();

        srcFiles.Length.Should().BeGreaterThan(1);
        dstFiles.Length.Should().Be(srcFiles.Length);

        double last = -1;
        foreach (var p in progresses)
        {
            p.Should().BeGreaterThanOrEqualTo(0.0);
            p.Should().BeLessThanOrEqualTo(1.0);
            p.Should().BeGreaterThanOrEqualTo(last);
            last = p;
        }

        output.WriteLine($"Upscaling {srcFiles.Length} images produced {progresses.Count} percentages.");
    }

    [Fact]
    public async Task UpscaleWithGPUControl()
    {
        const string INPUT_PATH = "./samples/single_4k.jpg";
        const string OUTPUT_PATH = "./single_upscaled.jpg";
        if (File.Exists(OUTPUT_PATH))
            File.Delete(OUTPUT_PATH);

        var args = new UpscaleInputArgs(EXEC, INPUT_PATH, OUTPUT_PATH);
        args.GPUs.Add(new GPUInfo(1));

        output.WriteLine(args.BuildArgs());

        var result = await UpscaleProcess.Run(args);

        result.ExitCode.Should().Be(0);
    }
}
