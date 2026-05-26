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
            throw new InvalidOperationException("There are no PDF files to merge.");
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
}
