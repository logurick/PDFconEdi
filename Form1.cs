using System.Diagnostics;

namespace ConcatPdfModern;

public sealed class Form1 : Form
{
    private readonly ListView fileList = new();
    private readonly StatusStrip statusStrip = new();
    private readonly ToolStripStatusLabel statusLabel = new();
    private readonly ToolStripStatusLabel pageCountLabel = new();
    private readonly Button addButton = new();
    private readonly Button removeButton = new();
    private readonly Button upButton = new();
    private readonly Button downButton = new();
    private readonly Button openButton = new();
    private readonly Button mergeButton = new();
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

        Text = "ConcatPDF Modern";
        MinimumSize = new Size(780, 480);
        Size = new Size(940, 620);
        StartPosition = FormStartPosition.CenterScreen;
        AllowDrop = true;
        DragEnter += Form_DragEnter;
        DragDrop += Form_DragDrop;

        var menu = BuildMenu();
        MainMenuStrip = menu;

        var toolbar = BuildToolbar();

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

        dropHint.Dock = DockStyle.Top;
        dropHint.Height = 38;
        dropHint.Text = "PDFファイルを追加して、順番を整えてから「結合して保存」を押してください。ドラッグ＆ドロップにも対応しています。";
        dropHint.TextAlign = ContentAlignment.MiddleLeft;
        dropHint.Padding = new Padding(12, 0, 12, 0);

        statusStrip.Items.Add(statusLabel);
        statusStrip.Items.Add(new ToolStripStatusLabel { Spring = true });
        statusStrip.Items.Add(pageCountLabel);

        Controls.Add(fileList);
        Controls.Add(dropHint);
        Controls.Add(toolbar);
        Controls.Add(menu);
        Controls.Add(statusStrip);

        UpdateCommands();

        ResumeLayout(false);
        PerformLayout();
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip();

        var fileMenu = new ToolStripMenuItem("ファイル(&F)");
        fileMenu.DropDownItems.Add("追加(&A)...", null, (_, _) => PickFiles());
        fileMenu.DropDownItems.Add("結合して保存(&S)...", null, (_, _) => SaveMergedPdf());
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("終了(&X)", null, (_, _) => Close());

        var editMenu = new ToolStripMenuItem("編集(&E)");
        editMenu.DropDownItems.Add("削除(&R)", null, (_, _) => RemoveSelected());
        editMenu.DropDownItems.Add(new ToolStripSeparator());
        editMenu.DropDownItems.Add("上へ(&U)", null, (_, _) => MoveSelected(-1));
        editMenu.DropDownItems.Add("下へ(&D)", null, (_, _) => MoveSelected(1));
        editMenu.DropDownItems.Add(new ToolStripSeparator());
        editMenu.DropDownItems.Add("すべて選択(&A)", null, (_, _) => SelectAllFiles());

        var viewMenu = new ToolStripMenuItem("表示(&V)");
        viewMenu.DropDownItems.Add("選択したPDFを開く(&O)", null, (_, _) => OpenSelectedFile());

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
            AutoScroll = true
        };

        ConfigureButton(addButton, "追加", (_, _) => PickFiles());
        ConfigureButton(removeButton, "削除", (_, _) => RemoveSelected());
        ConfigureButton(upButton, "上へ", (_, _) => MoveSelected(-1));
        ConfigureButton(downButton, "下へ", (_, _) => MoveSelected(1));
        ConfigureButton(openButton, "開く", (_, _) => OpenSelectedFile());
        ConfigureButton(mergeButton, "結合して保存", (_, _) => SaveMergedPdf(), width: 132);

        toolbar.Controls.AddRange([addButton, removeButton, upButton, downButton, openButton, mergeButton]);
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
            Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*",
            Multiselect = true,
            RestoreDirectory = true,
            Title = "結合するPDFファイルを選択"
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
                var entry = PdfMerger.ReadEntry(path);
                files.Add(entry);
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

    private void SaveMergedPdf()
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
            Filter = "PDF files (*.pdf)|*.pdf",
            FileName = "merged.pdf",
            OverwritePrompt = true,
            RestoreDirectory = true,
            Title = "結合したPDFの保存先"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            return;
        }

        Cursor = Cursors.WaitCursor;
        SetStatus("PDFを結合しています...");

        try
        {
            PdfMerger.Merge(files, dialog.FileName);
            SetStatus($"保存しました: {dialog.FileName}");
            MessageBox.Show(this, "PDFの結合が完了しました。", "ConcatPDF Modern", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            SetStatus("結合に失敗しました。");
            MessageBox.Show(this, ex.Message, "PDFを結合できません", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
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
            item.SubItems.Add(entry.PageCount.ToString());
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
        UpdateCommands();
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
        mergeButton.Enabled = hasFiles;

        pageCountLabel.Text = $"{files.Count}ファイル / {files.Sum(file => file.PageCount)}ページ";
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
            "ConcatPDF Modern\n\n.NETの現行Windowsデスクトップ環境向けに作り直したPDF結合ツールです。\nPDF操作には PDFsharp を使用しています。",
            "ConcatPDF Modern",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }
}
