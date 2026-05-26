using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace ConcatPdfModern;

public static class PdfMerger
{
    private const string AppName = "PDFconEdi";

    public static PdfFileEntry ReadEntry(string path)
    {
        using var document = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        return new PdfFileEntry(path, document.PageCount);
    }

    public static void Merge(IEnumerable<PdfFileEntry> entries, string outputPath)
    {
        var inputFiles = entries.ToList();
        if (inputFiles.Count == 0)
        {
            throw new InvalidOperationException("結合するPDFがありません。");
        }

        using var output = new PdfDocument();
        output.Info.Title = Path.GetFileNameWithoutExtension(outputPath);
        output.Info.Creator = AppName;

        foreach (var entry in inputFiles)
        {
            using var input = PdfReader.Open(entry.Path, PdfDocumentOpenMode.Import);
            for (var index = 0; index < input.PageCount; index++)
            {
                output.AddPage(input.Pages[index]);
            }
        }

        output.Save(outputPath);
    }

    public static IReadOnlyList<string> Split(PdfFileEntry entry, string outputDirectory)
    {
        Directory.CreateDirectory(outputDirectory);

        using var input = PdfReader.Open(entry.Path, PdfDocumentOpenMode.Import);
        var createdFiles = new List<string>(input.PageCount);
        var baseName = Path.GetFileNameWithoutExtension(entry.Path);
        var digits = Math.Max(3, input.PageCount.ToString().Length);

        for (var index = 0; index < input.PageCount; index++)
        {
            using var output = new PdfDocument();
            output.Info.Title = $"{baseName} {index + 1}";
            output.Info.Creator = AppName;
            output.AddPage(input.Pages[index]);

            var outputPath = CreateSplitFilePath(outputDirectory, baseName, index + 1, digits);
            output.Save(outputPath);
            createdFiles.Add(outputPath);
        }

        return createdFiles;
    }

    private static string CreateSplitFilePath(string outputDirectory, string baseName, int pageNumber, int digits)
    {
        var suffix = pageNumber.ToString($"D{digits}");
        var candidate = Path.Combine(outputDirectory, $"{baseName}_{suffix}.pdf");
        if (!File.Exists(candidate))
        {
            return candidate;
        }

        for (var copy = 2; ; copy++)
        {
            candidate = Path.Combine(outputDirectory, $"{baseName}_{suffix}_{copy}.pdf");
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }
}
