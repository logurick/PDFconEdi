using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace ConcatPdfModern;

public static class PdfMerger
{
    private const string AppName = "PDFconEdi";

    public static PdfFileEntry ReadEntry(string path)
    {
        using var document = PdfReader.Open(path, PdfDocumentOpenMode.Import);
        return new PdfFileEntry(path, document.PageCount, 1, document.PageCount);
    }

    public static void Save(IEnumerable<PdfFileEntry> entries, string outputPath)
    {
        var inputEntries = entries.ToList();
        if (inputEntries.Count == 0)
        {
            throw new InvalidOperationException("保存するPDFがありません。");
        }

        using var output = new PdfDocument();
        output.Info.Title = Path.GetFileNameWithoutExtension(outputPath);
        output.Info.Creator = AppName;

        foreach (var entry in inputEntries)
        {
            using var input = PdfReader.Open(entry.Path, PdfDocumentOpenMode.Import);
            for (var pageNumber = entry.StartPage; pageNumber <= entry.EndPage; pageNumber++)
            {
                output.AddPage(input.Pages[pageNumber - 1]);
            }
        }

        output.Save(outputPath);
    }

    public static IReadOnlyList<PdfFileEntry> SplitIntoPages(PdfFileEntry entry)
    {
        return Enumerable
            .Range(entry.StartPage, entry.PageCount)
            .Select(pageNumber => entry with { StartPage = pageNumber, EndPage = pageNumber })
            .ToList();
    }
}
