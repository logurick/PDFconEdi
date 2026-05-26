namespace ConcatPdfModern;

public sealed record PdfFileEntry(string Path, int SourcePageCount, int StartPage, int EndPage)
{
    public string FileName => System.IO.Path.GetFileName(Path);

    public int PageCount => EndPage - StartPage + 1;

    public string PageLabel => StartPage == EndPage ? StartPage.ToString() : $"{StartPage}-{EndPage}";

    public bool CanSplit => PageCount > 1;
}
