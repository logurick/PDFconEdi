using System.Diagnostics;

namespace ConcatPdfModern;

public sealed class Form1 : Form
{
    private const string AppName = "PDFconEdi";

    private readonly ListView fileList = new();
    private readonly StatusStrip statusStrip = new();
    private readonly ToolStripStatusLabel statusLabel = new();
    private readonly ToolStripStatusLabel pageCountLabel = new();
    private readonly Button addButton = new();
    private readonly Button removeButton = new();
    private readonly Button upButton = new();
    private readonly Button downButton = new();
    private readonly Button openButton = new();
    private readonly Button splitButton = new();
    private readonly Button saveButton = new();
    private readonly Label dropHint = new();

    private readonly List<PdfFileEntry> files = [];

    public Form1(IEnumerable<string>? startupFiles = null)
    {
        InitializeComponent();

        if (startupFiles is not null)
        {
            AddFiles(startupFiles);
        }
    }

    private void InitializeComponent()
    {
        SuspendLayout();

        Text = AppName;
        MinimumSize = new Size(780, 480);
        Size = new Size(940, 620);
        StartPosition = FormStartPosition.CenterScreen;
        AllowDrop = true;
        DragEnter += Form_DragEnter;
        DragDrop += Form_DragDrop;

        var menu = BuildMenu();
        MainMenuStrip = menu;

        fileList.Dock = DockStyle.Fill;
        fileList.View = View.Details;
        fileList.FullRowSelect = true;
        fileList.HideSelection = false;
        fileList.MultiSelect = true;
        fileList.AllowDrop = true;
        fileList.Columns.Add("ファイル名", 320);
        fileList.Columns.Add("ページ", 80, HorizontalAlignment.Right);
        fileList.Columns.Add("フォルダ", 460);
        fileList.SelectedIndexChanged += (_, _) => UpdateCommands();
        fileList.DoubleClick += (_, _) => OpenSelectedFile();
        fileList.DragEnter += Form_DragEnter;
        fileList.DragDrop += Form_DragDrop;
        fileList.SizeChanged += (_, _) => AdjustFileListColumns();

        dropHint.Dock = DockStyle.Top;
        dropHint.Height = 38;
        dropHint.Text = "PDFを追加し、必要に応じて一覧上で分割してから「保存」を押してください。";
        dropHint.TextAlign = ContentAlignment.MiddleLeft;
        dropHint.Padding = new Padding(12, 0, 12, 0);

        statusStrip.Items.Add(statusLabel);
        statusStrip.Items.Add(new ToolStripStatusLabel { Spring = true });
        statusStrip.Items.Add(pageCountLabel);

        Controls.Add(fileList);
        Controls.Add(dropHint);
        Controls.Add(BuildToolbar());
        Controls.Add(menu);
        Controls.Add(statusStrip);

        SetStatus("準備完了。");

        ResumeLayout(false);
        PerformLayout();
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip();

        var fileMenu = new ToolStripMenuItem("ファイル(&F)");
        fileMenu.DropDownItems.Add("追加(&A)...", null, (_, _) => PickFiles());
        fileMenu.DropDownItems.Add("保存(&S)...", null, (_, _) => SavePdf());
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("終了(&X)", null, (_, _) => Close());

        var editMenu = new ToolStripMenuItem("編集(&E)");
        editMenu.DropDownItems.Add("選択PDFを分割(&P)", null, (_, _) => SplitSelectedPdfs());
        editMenu.DropDownItems.Add("削除(&R)", null, (_, _) => RemoveSelected());
        editMenu.DropDownItems.Add(new ToolStripSeparator());
        editMenu.DropDownItems.Add("上へ移動(&U)", null, (_, _) => MoveSelected(-1));
        editMenu.DropDownItems.Add("下へ移動(&D)", null, (_, _) => MoveSelected(1));
        editMenu.DropDownItems.Add(new ToolStripSeparator());
        editMenu.DropDownItems.Add("すべて選択(&A)", null, (_, _) => SelectAllFiles());

        var viewMenu = new ToolStripMenuItem("表示(&V)");
        viewMenu.DropDownItems.Add("選択PDFを開く(&O)", null, (_, _) => OpenSelectedFile());

        var helpMenu = new ToolStripMenuItem("ヘルプ(&H)");
        helpMenu.DropDownItems.Add("このアプリについて(&A)", null, (_, _) => ShowAbout());

        menu.Items.AddRange([fileMenu, editMenu, viewMenu, helpMenu]);
        return menu;
    }

    private FlowLayoutPanel BuildToolbar()
    {
        var toolbar = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            Height = 48,
            Padding = new Padding(8, 8, 8, 4),
            WrapContents = false,
            AutoScroll = false
        };

        ConfigureButton(addButton, "追加", (_, _) => PickFiles());
        ConfigureButton(removeButton, "削除", (_, _) => RemoveSelected());
        ConfigureButton(upButton, "上へ", (_, _) => MoveSelected(-1));
        ConfigureButton(downButton, "下へ", (_, _) => MoveSelected(1));
        ConfigureButton(openButton, "開く", (_, _) => OpenSelectedFile());
        ConfigureButton(splitButton, "分割", (_, _) => SplitSelectedPdfs());
        ConfigureButton(saveButton, "保存", (_, _) => SavePdf(), width: 96);

        toolbar.Controls.AddRange([addButton, removeButton, upButton, downButton, openButton, splitButton, saveButton]);
        return toolbar;
    }

    private static void ConfigureButton(Button button, string text, EventHandler onClick, int width = 84)
    {
        button.Text = text;
        button.Width = width;
        button.Height = 30;
        button.Margin = new Padding(0, 0, 8, 0);
        button.Click += onClick;
    }

    private void PickFiles()
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "PDFファイル (*.pdf)|*.pdf|すべてのファイル (*.*)|*.*",
            Multiselect = true,
            RestoreDirectory = true,
            Title = "PDFファイルを選択"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            AddFiles(dialog.FileNames);
        }
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        var added = 0;

        foreach (var path in paths.Where(File.Exists))
        {
            if (!Path.GetExtension(path).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            try
            {
                files.Add(PdfMerger.ReadEntry(path));
                added++;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"{path}\n\n{ex.Message}", "PDFを追加できません", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        RefreshFileList();
        SetStatus(added == 0 ? "追加できるPDFがありませんでした。" : $"{added}件のPDFを追加しました。");
    }

    private void SavePdf()
    {
        if (files.Count == 0)
        {
            SetStatus("PDFを追加してください。");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            AddExtension = true,
            DefaultExt = "pdf",
            Filter = "PDFファイル (*.pdf)|*.pdf",
            FileName = "保存.pdf",
            OverwritePrompt = true,
            RestoreDirectory = true,
            Title = "PDFの保存先"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        Cursor = Cursors.WaitCursor;
        SetStatus("PDFを保存しています...");

        try
        {
            PdfMerger.Save(files, dialog.FileName);
            SetStatus($"保存しました: {dialog.FileName}");
            MessageBox.Show(this, "PDFの保存が完了しました。", AppName, MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            SetStatus("保存に失敗しました。");
            MessageBox.Show(this, ex.Message, "PDFを保存できません", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    private void SplitSelectedPdfs()
    {
        if (fileList.SelectedIndices.Count == 0)
        {
            SetStatus("分割するPDFを選択してください。");
            return;
        }

        var selectedIndexes = fileList.SelectedIndices.Cast<int>().OrderDescending().ToArray();
        var splitCount = 0;

        foreach (var index in selectedIndexes)
        {
            var entry = files[index];
            if (!entry.CanSplit)
            {
                continue;
            }

            files.RemoveAt(index);
            files.InsertRange(index, PdfMerger.SplitIntoPages(entry));
            splitCount++;
        }

        RefreshFileList();
        SetStatus(splitCount == 0 ? "分割できる複数ページの項目がありません。" : $"{splitCount}件のPDFを一覧上で分割しました。");
    }

    private void RemoveSelected()
    {
        var selectedIndexes = fileList.SelectedIndices.Cast<int>().OrderDescending().ToArray();
        foreach (var index in selectedIndexes)
        {
            files.RemoveAt(index);
        }

        RefreshFileList();
        SetStatus($"{selectedIndexes.Length}件を削除しました。");
    }

    private void MoveSelected(int direction)
    {
        if (fileList.SelectedIndices.Count == 0)
        {
            return;
        }

        var selected = fileList.SelectedIndices.Cast<int>().Order().ToList();
        if (direction < 0 && selected.First() == 0)
        {
            return;
        }

        if (direction > 0 && selected.Last() == files.Count - 1)
        {
            return;
        }

        if (direction > 0)
        {
            selected.Reverse();
        }

        foreach (var index in selected)
        {
            (files[index], files[index + direction]) = (files[index + direction], files[index]);
        }

        RefreshFileList(selected.Select(index => index + direction));
        SetStatus("順番を変更しました。");
    }

    private void SelectAllFiles()
    {
        foreach (ListViewItem item in fileList.Items)
        {
            item.Selected = true;
        }
    }

    private void OpenSelectedFile()
    {
        if (fileList.SelectedIndices.Count == 0)
        {
            return;
        }

        var path = files[fileList.SelectedIndices[0]].Path;
        try
        {
            Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "PDFを開けません", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void RefreshFileList(IEnumerable<int>? selectedIndexes = null)
    {
        var selected = selectedIndexes?.ToHashSet() ?? [];

        fileList.BeginUpdate();
        fileList.Items.Clear();

        foreach (var entry in files)
        {
            var item = new ListViewItem(entry.FileName);
            item.SubItems.Add(entry.PageLabel);
            item.SubItems.Add(Path.GetDirectoryName(entry.Path) ?? "");
            fileList.Items.Add(item);
        }

        foreach (var index in selected)
        {
            if (index >= 0 && index < fileList.Items.Count)
            {
                fileList.Items[index].Selected = true;
                fileList.Items[index].Focused = true;
            }
        }

        fileList.EndUpdate();
        AdjustFileListColumns();
        UpdateCommands();
    }

    private void AdjustFileListColumns()
    {
        if (fileList.Columns.Count < 3 || fileList.ClientSize.Width <= 0)
        {
            return;
        }

        var availableWidth = fileList.ClientSize.Width - 4;
        if (fileList.Items.Count > fileList.ClientSize.Height / Math.Max(fileList.Font.Height + 4, 1))
        {
            availableWidth -= SystemInformation.VerticalScrollBarWidth;
        }

        availableWidth = Math.Max(availableWidth, 360);

        var pagesWidth = 72;
        var fileWidth = Math.Max(150, (int)(availableWidth * 0.42));
        var folderWidth = Math.Max(120, availableWidth - fileWidth - pagesWidth);

        fileList.Columns[0].Width = fileWidth;
        fileList.Columns[1].Width = pagesWidth;
        fileList.Columns[2].Width = folderWidth;
    }

    private void UpdateCommands()
    {
        var hasFiles = files.Count > 0;
        var hasSelection = fileList.SelectedIndices.Count > 0;
        var canMoveUp = hasSelection && fileList.SelectedIndices.Cast<int>().Min() > 0;
        var canMoveDown = hasSelection && fileList.SelectedIndices.Cast<int>().Max() < files.Count - 1;

        removeButton.Enabled = hasSelection;
        upButton.Enabled = canMoveUp;
        downButton.Enabled = canMoveDown;
        openButton.Enabled = hasSelection;
        splitButton.Enabled = hasSelection && fileList.SelectedIndices.Cast<int>().Any(index => files[index].CanSplit);
        saveButton.Enabled = hasFiles;

        pageCountLabel.Text = $"{files.Count}項目 / {files.Sum(file => file.PageCount)}ページ";
    }

    private void SetStatus(string message)
    {
        statusLabel.Text = message;
        UpdateCommands();
    }

    private static void Form_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
        {
            e.Effect = DragDropEffects.Copy;
        }
    }

    private void Form_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
        {
            AddFiles(paths);
        }
    }

    private void ShowAbout()
    {
        MessageBox.Show(
            this,
            "PDFconEdi\n\nPDFファイルを一覧上で編集し、保存時に1つのPDFとして出力するWindowsデスクトップアプリです。\nPDF処理にはPDFsharpを使用しています。",
            AppName,
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}
