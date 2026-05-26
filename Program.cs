namespace ConcatPdfModern;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main(string[] args)
    {
        if (TryRunCommandLine(args))
        {
            return;
        }

        // To customize application configuration such as set high DPI settings or default font,
        // see https://aka.ms/applicationconfiguration.
        ApplicationConfiguration.Initialize();
        Application.Run(new Form1(args));
    }

    private static bool TryRunCommandLine(string[] args)
    {
        if (args.Length < 3)
        {
            return false;
        }

        var outputIndex = Array.FindIndex(args, arg => arg is "--output" or "-o");
        if (outputIndex < 0 || outputIndex + 1 >= args.Length)
        {
            return false;
        }

        var outputPath = args[outputIndex + 1];
        var inputPaths = args
            .Where((_, index) => index != outputIndex && index != outputIndex + 1)
            .ToArray();

        var entries = inputPaths.Select(PdfMerger.ReadEntry).ToList();
        PdfMerger.Save(entries, outputPath);
        return true;
    }
}
