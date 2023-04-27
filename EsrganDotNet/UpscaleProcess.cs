using System.Diagnostics;

namespace RealESRGAN;

/// <summary>
/// A utility class that offers the <see cref="Run(RealESRGAN.UpscaleInputArgs, Action{double}?, CancellationToken)"/> method which acts as a thin wrapper around the RealESRGAN compiled binaries for windows, linux or mac.
/// </summary>
public static class UpscaleProcess
{
    /// <summary>
    /// Asynchronously starts a new process to upscale 1 or more images using RealESRGAN.
    /// The upscaling operation can be cancelled at any point using the <paramref name="cancellationToken"/>, but doing so may leave the output image(s)
    /// in a corrupted or incomplete state.
    /// The <paramref name="onProgress"/> action may be used to get a progress report of the operation. For a single-image operation, the progress will be accurate for that single image.
    /// For multi-image operations, the resolution of the reported percentage will only be accurate
    /// to the closest 100/n percent, where n is the number of images.
    /// </summary>
    /// <param name="args">The input parameters for the operation. At a bare minumum, <see cref="UpscaleInputArgs.ExecutablePath"/>, <see cref="UpscaleInputArgs.InputPath"/> and <see cref="UpscaleInputArgs.OutputPath"/> must be specified.</param>
    /// <param name="onProgress">An option action that is invoked to report progress on the operation. For single-image operations, progress may be reported many times per second. For multi-image operations, progress is only reported when an entire image finishes upscaling.</param>
    /// <param name="cancellationToken">An optional cancellation token. The operation may be cancelled at any point but may leave the output image(s) in a corrupt or incomplete state. Cancelling the operation will not throw an exception, but the return value exit code will be non-zero.</param>
    /// <returns>The process exit code. Non-zero <see cref="Result.ExitCode"/> indicates failure.</returns>
    /// <exception cref="ArgumentNullException">If <paramref name="args"/> is null.</exception>
    /// <exception cref="FileNotFoundException">If <see cref="UpscaleInputArgs.InputPath"/> cannot be found.</exception>
    /// <exception cref="Exception">If the process fails to start for an unknown reason.</exception>
    public static async Task<Result> Run(UpscaleInputArgs args, Action<double>? onProgress = null, CancellationToken cancellationToken = default)
    {
        if (args == null)
            throw new ArgumentNullException(nameof(args));

        if (cancellationToken.IsCancellationRequested)
            return new Result { ExitCode = -1, ErrorMessage = "Operation cancelled" };

        string cmdArgs = args.BuildArgs();

        bool isDir = Directory.Exists(args.InputPath);
        bool isFile = File.Exists(args.InputPath);

        if (!isDir && !isFile)
            throw new FileNotFoundException("Failed to find input directory/file", new FileInfo(args.InputPath).FullName);

        CreateDir(isDir ? new DirectoryInfo(args.OutputPath) : new FileInfo(args.OutputPath).Directory);

        var outputLines = new Queue<string>(16);
        int filesToUpscale = isFile ? 1 : Directory.EnumerateFiles(args.InputPath, "*.*", SearchOption.TopDirectoryOnly).Count();
        int filesCreated = 0;
        FileSystemWatcher? watcher = null;
        if (onProgress != null && isDir)
        {
            watcher = new FileSystemWatcher(args.OutputPath);
            watcher.Created += (_, _) =>
            {
                filesCreated++;
                onProgress((double)filesCreated / filesToUpscale);
            };
            watcher.EnableRaisingEvents = true;
        }

        void OnOutputLine(string line)
        {
            bool isPct = true;
            if (line[^1] != '%')
            {
                isPct = false;
                if (outputLines.Count == 16)
                    outputLines.Dequeue();
                outputLines.Enqueue(line);
            }

            if (isDir || !isPct)
                return;

            // Progress is reported in the form 53.23% from esrgan.
            onProgress(double.Parse(line[..^2]) * 0.01);
        }

        try
        {
            var procArgs = new ProcessStartInfo
            {
                FileName = args.ExecutablePath,
                Arguments = cmdArgs,
                RedirectStandardError = true,
                UseShellExecute = false
            };

            using var proc = Process.Start(procArgs) ?? throw new Exception("Unable to start upscaler process, reason unknown.");
            proc.BeginErrorReadLine();

            proc.ErrorDataReceived += (_, e) =>
            {
                if (e.Data == null || onProgress == null)
                    return;

                string line = e.Data.TrimEnd();
                OnOutputLine(line);
            };

            try
            {
                await proc.WaitForExitAsync(cancellationToken);
            }
            catch (TaskCanceledException) {  }

            if (cancellationToken.IsCancellationRequested)
                proc.Kill(true);

            return new Result
            {
                ExitCode = proc.ExitCode,
                ErrorMessage = proc.ExitCode == 0 ? null : string.Join('\n', outputLines.Reverse())
            };
        }
        finally
        {
            watcher?.Dispose();
        }
    }

    private static void CreateDir(DirectoryInfo? dir)
    {
        if (dir == null || dir.Exists)
            return;

        CreateDir(dir.Parent);
        dir.Create();
    }

    /// <summary>
    /// The result of an upscaling operation.
    /// </summary>
    public readonly struct Result
    {
        /// <summary>
        /// The exit code of the process.
        /// Any non-zero value indicates failure or error.
        /// </summary>
        public int ExitCode { get; init; }
        /// <summary>
        /// An error message as reported by the RESRGAN process.
        /// Note: there is currently a .NET bug that means that the error message is sometimes not fully captured.
        /// </summary>
        public string? ErrorMessage { get; init; }
    }
}
