# PDFconEdi

PDFconEdi is a modern Windows desktop app for combining PDF files.

This repository rebuilds the core PDF concatenation workflow from ConcatPDF for current Windows desktop environments. It starts with a focused WinForms application for the most common workflow:

- add PDF files
- reorder them
- remove selected files
- open a selected source PDF
- split selected PDFs into page items in the list
- save the edited list into one PDF

## Requirements

- Windows
- .NET SDK 10 or later for development

The app targets `net10.0-windows` and does not require .NET Framework 3.5.

## Build

```powershell
dotnet build
```

## Run

```powershell
dotnet run
```

## Command Line

```powershell
dotnet run -- --output merged.pdf input1.pdf input2.pdf
```

PDF handling is implemented with PDFsharp.
