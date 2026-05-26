namespace ConcatPdfModern;

public sealed record PdfFileEntry(string Path, int PageCount)
{
    public string FileName => System.IO.Path.GetFileName(Path);
}
