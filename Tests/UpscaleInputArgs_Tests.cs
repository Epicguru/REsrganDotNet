using RealESRGAN;
using Xunit.Abstractions;

namespace Tests;

public class UpscaleInputArgs_Tests
{
    private readonly ITestOutputHelper output;

    public UpscaleInputArgs_Tests(ITestOutputHelper output)
    {
        this.output = output;
    }

    [Fact]
    public void BuildCommandArgs()
    {
        var args = new UpscaleInputArgs("./windows/realesrgan-ncnn-vulkan", "./input", "./output");

        string built = args.BuildArgs();
        Assert.NotNull(built);
        Assert.False(string.IsNullOrWhiteSpace(built));

        output.WriteLine(built);
    }
}
