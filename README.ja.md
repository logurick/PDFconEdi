# PDFconEdi

PDFconEdi は、PDFファイルを結合・編集するためのWindowsデスクトップアプリです。

旧 ConcatPDF の基本的なPDF結合ワークフローを、現在のWindows環境向けに作り直しています。.NET Framework 3.5 は不要です。

## 主な機能

- PDFファイルの追加
- 一覧上での並べ替え
- 選択項目の削除
- 選択したPDFの表示
- 複数ページPDFを一覧上でページ単位に分割
- 編集した一覧を1つのPDFとして保存

分割操作をしても、その時点ではPDFファイルは作成しません。一覧上の項目だけをページ単位に展開し、最後に「保存」を押したときに現在の一覧内容どおりのPDFを出力します。

## 必要環境

- Windows
- 開発時は .NET SDK 10 以降

アプリは `net10.0-windows` を対象にしています。

## ビルド

```powershell
dotnet build
```

## 実行

```powershell
dotnet run
```

## コマンドライン実行

```powershell
dotnet run -- --output merged.pdf input1.pdf input2.pdf
```

PDF処理には PDFsharp を使用しています。
