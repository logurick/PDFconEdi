# ConcatPDF Modern

ConcatPDF Modern is a rebuild of the core PDF concatenation workflow from ConcatPDF for current Windows desktop environments.

The original ConcatPDF supported many operations, including concatenation, extraction, encryption, decryption, viewer settings, and image conversion. This repository starts with a small, modern WinForms application focused on the most common workflow:

- add PDF files
- reorder them
- remove selected files
- open a selected source PDF
- merge all files into one PDF

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

PDF handling is implemented with PDFsharp.
