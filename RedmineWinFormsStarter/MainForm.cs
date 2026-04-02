using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RedmineWinFormsStarter;

public partial class MainForm : Form
{
    // LINKTAG RMWIN001
    private static readonly string[] CreateIssueColumns =
    [
        "プロジェクト",
        "トラッカー",
        "親チケット",
        "ステータス",
        "優先度",
        "題名",
        "担当者",
        "カテゴリ",
        "対象バージョン",
        "開始日",
        "期日",
        "進捗率",
        "プライベート",
        "説明"
    ];

    private DataTable? _csv;
    private DataView? _csvView;
    private CsvWorkspaceSession? _csvSession;
    private DataTable? _commentsTable;
    private DataTable? _createIssuesTable;
    private readonly Dictionary<string, TextBox> _filterInputs = new(StringComparer.OrdinalIgnoreCase);
    private RedmineFieldOptionsService? _fieldOptionsService;
    private string? _serviceBaseUrl;
    private string? _serviceApiKey;
    private bool _applyingCsvConstraints;
    private bool _applyingCreateConstraints;

    public MainForm()
    {
        InitializeComponent();
        txtRedmineUrl.Text = Properties.Settings.Default.RedmineUrl;
        txtApiKey.Text = Properties.Settings.Default.ApiKey;
        cmbCreateProject.DisplayMember = nameof(RedmineProjectChoice.Name);
        cmbCreateProject.ValueMember = nameof(RedmineProjectChoice.Identifier);

        HookIssueGridEvents(gridCsv, gridCsv_CellValueChanged, gridCsv_CellValidating, gridCsv_CurrentCellDirtyStateChanged, gridCsv_DataError);
        HookIssueGridEvents(gridCreateIssues, gridCreateIssues_CellValueChanged, gridCreateIssues_CellValidating, gridCreateIssues_CurrentCellDirtyStateChanged, gridCreateIssues_DataError);
        gridCreateIssues.DefaultValuesNeeded += gridCreateIssues_DefaultValuesNeeded;
        gridCreateIssues.CellMouseDown += gridCreateIssues_CellMouseDown;
        gridCreateIssues.KeyDown += gridCreateIssues_KeyDown;
        cmsCreateRows.Opening += cmsCreateRows_Opening;
        gridCsv.CellMouseDown += gridCsv_CellMouseDown;
        cmsIssueCell.Opening += cmsIssueCell_Opening;

        ConfigureCommentsGrid();
        ConfigureIssueGrid(gridCsv);
        ConfigureIssueGrid(gridCreateIssues);
        ResetCreateIssueTable();
        BindCreateProjects([]);
        SetCommentsStatus("コメントはまだ取得していません。");
        UpdateCsvInfo();
    }

    private bool ValidateInputs()
    {
        if (string.IsNullOrWhiteSpace(txtRedmineUrl.Text) || string.IsNullOrWhiteSpace(txtApiKey.Text))
        {
            MessageBox.Show("RedmineURL と APIキー は必須です", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        if (_csvSession is null)
        {
            MessageBox.Show("CSVを先に読み込んでください", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        return true;
    }

    private void SaveSettings()
    {
        Properties.Settings.Default.RedmineUrl = txtRedmineUrl.Text;
        Properties.Settings.Default.ApiKey = txtApiKey.Text;
        Properties.Settings.Default.Save();
    }

    private void AppendLog(string line)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<string>(AppendLog), line);
            return;
        }

        txtLog.AppendText(line + Environment.NewLine);
    }

    private async void btnLoadCsv_Click(object? sender, EventArgs e)
    {
        // LINKTAG RMCSV001
        // LINKTAG RMCSV002
        // LINKTAG RMCSV003
        try
        {
            SaveSettings();

            bool remoteAvailable = false;
            IReadOnlyList<RedmineProjectChoice> projectChoices = [];

            try
            {
                RedmineFieldOptionsService? service = await GetOrCreateRedmineServiceAsync(requireCredentials: false, CancellationToken.None);
                remoteAvailable = service is not null;
                projectChoices = service?.GetProjectChoices() ?? [];
            }
            catch (Exception ex)
            {
                AppendLog($"[Redmine] 読み込み候補の初期化に失敗しました。ローカルCSVのみ利用できます: {ex.Message}");
            }

            using CsvLoadDialog dialog = new(projectChoices, remoteAvailable);
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            _csvSession = await LoadCsvSessionAsync(dialog, CancellationToken.None);
            BindCsvTable(_csvSession.Table);
            UpdateCsvInfo();
            AppendLog($"[CSV] Loaded: {_csvSession.SourceDescription} encoding={_csvSession.DetectedEncoding} working={_csvSession.WorkingFilePath} rows={_csvSession.Table.Rows.Count}");

            await RefreshCsvConstraintsAsync(CancellationToken.None);
            await RefreshCommentsAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "CSV Load Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnSaveCsv_Click(object? sender, EventArgs e)
    {
        // LINKTAG RMCSV002
        try
        {
            if (!SaveCsvToDisk())
            {
                return;
            }

            MessageBox.Show("CSVを保存しました", "Save", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "CSV Save Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnDryRun_Click(object? sender, EventArgs e)
    {
        // LINKTAG RMBAT001
        // LINKTAG RMBAT003
        if (!ValidateInputs())
        {
            return;
        }

        SaveSettings();

        try
        {
            if (!SaveCsvToDisk())
            {
                return;
            }

            BatRunner.Start("run_dryrun.bat", txtRedmineUrl.Text, txtApiKey.Text, _csvSession!.WorkingFilePath, AppendLog);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Run Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void btnUpdate_Click(object? sender, EventArgs e)
    {
        // LINKTAG RMBAT002
        // LINKTAG RMBAT003
        if (!ValidateInputs())
        {
            return;
        }

        SaveSettings();

        try
        {
            if (!SaveCsvToDisk())
            {
                return;
            }

            BatRunner.Start("run_update.bat", txtRedmineUrl.Text, txtApiKey.Text, _csvSession!.WorkingFilePath, AppendLog);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Run Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void btnRefreshComments_Click(object? sender, EventArgs e)
    {
        // LINKTAG RMCMT001
        try
        {
            await RefreshCommentsAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Comments Refresh Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private async void btnApplyCommentUpdates_Click(object? sender, EventArgs e)
    {
        // LINKTAG RMCMT001
        try
        {
            using AddCommentDialog dialog = new();
            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            RedmineFieldOptionsService service = await GetOrCreateRedmineServiceAsync(requireCredentials: true, CancellationToken.None)
                ?? throw new InvalidOperationException("Redmine 接続情報を設定してください。");

            UseWaitCursor = true;
            AppendLog($"[Redmine] コメントを追記しています... issue={dialog.IssueId}");
            await service.AddIssueCommentAsync(dialog.IssueId, dialog.CommentText, CancellationToken.None);
            AppendLog($"[Redmine] コメントを追記しました: issue={dialog.IssueId}");
            await RefreshCommentsAsync(CancellationToken.None);
            MessageBox.Show("コメントを追記しました。", "Comments", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Comments Append Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private async void btnCreateIssues_Click(object? sender, EventArgs e)
    {
        // LINKTAG RMCRE001
        try
        {
            RedmineFieldOptionsService service = await GetOrCreateRedmineServiceAsync(requireCredentials: true, CancellationToken.None)
                ?? throw new InvalidOperationException("Redmine 接続情報を設定してください。");

            EndGridEdit(gridCreateIssues);
            List<IReadOnlyDictionary<string, string>> drafts = GetCreateIssueDrafts();
            if (drafts.Count == 0)
            {
                MessageBox.Show("作成対象の行がありません。", "Create Issues", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            UseWaitCursor = true;
            AppendLog($"[Redmine] チケットを作成しています... count={drafts.Count}");
            IReadOnlyList<CreatedIssueInfo> createdIssues = await service.CreateIssuesAsync(drafts, CancellationToken.None);
            AppendCreatedIssuesToCsv(createdIssues);
            ResetCreateIssueTable();
            AppendLog($"[Redmine] チケットを作成しました: count={createdIssues.Count}");
            await RefreshCommentsAsync(CancellationToken.None);
            MessageBox.Show($"{createdIssues.Count} 件のチケットを作成しました。", "Create Issues", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Create Issues Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void btnResetCreateIssues_Click(object? sender, EventArgs e)
    {
        ResetCreateIssueTable();
    }

    private void btnClearCsvFilters_Click(object? sender, EventArgs e)
    {
        foreach (TextBox textBox in _filterInputs.Values)
        {
            textBox.Text = string.Empty;
        }
    }

    private async void cmbCreateProject_SelectedIndexChanged(object? sender, EventArgs e)
    {
        // LINKTAG RMMETA001
        try
        {
            string projectName = GetSelectedCreateProjectName();
            if (string.IsNullOrWhiteSpace(projectName) || _createIssuesTable is null)
            {
                return;
            }

            foreach (DataRow row in _createIssuesTable.Rows)
            {
                row["プロジェクト"] = projectName;
                ClearCreateIssueDependentFields(row);
            }

            if (_createIssuesTable.Rows.Count == 0)
            {
                DataRow row = _createIssuesTable.NewRow();
                row["プロジェクト"] = projectName;
                _createIssuesTable.Rows.Add(row);
            }

            await RefreshCreateGridConstraintsAsync(CancellationToken.None);
        }
        catch (Exception ex)
        {
            AppendLog($"[Redmine] 作成タブの候補更新に失敗しました: {ex.Message}");
        }
    }

    private void gridCreateIssues_DefaultValuesNeeded(object? sender, DataGridViewRowEventArgs e)
    {
        string projectName = GetSelectedCreateProjectName();
        if (!string.IsNullOrWhiteSpace(projectName) && e.Row.Cells["プロジェクト"] is DataGridViewCell cell)
        {
            cell.Value = projectName;
        }
    }

    private async Task<CsvWorkspaceSession> LoadCsvSessionAsync(CsvLoadDialog dialog, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dialog);

        return dialog.SelectedMode switch
        {
            CsvLoadMode.LocalFile => CsvWorkspaceSession.ImportLocalFile(dialog.LocalPath),
            CsvLoadMode.DownloadFromUrl => await LoadCsvFromUrlAsync(dialog.DownloadUrl, cancellationToken),
            CsvLoadMode.DownloadFromProject => await LoadCsvFromProjectAsync(dialog.SelectedProject, cancellationToken),
            _ => throw new InvalidOperationException("未対応のCSV読み込み方法です。")
        };
    }

    private async Task<CsvWorkspaceSession> LoadCsvFromUrlAsync(string url, CancellationToken cancellationToken)
    {
        RedmineFieldOptionsService service = await GetOrCreateRedmineServiceAsync(requireCredentials: true, cancellationToken)
            ?? throw new InvalidOperationException("Redmine 接続情報を設定してください。");

        byte[] bytes = await service.DownloadIssuesCsvAsync(url, cancellationToken);
        return CsvWorkspaceSession.ImportDownloadedBytes(bytes, $"Redmine URL CSV: {url}", "redmine_url_download");
    }

    private async Task<CsvWorkspaceSession> LoadCsvFromProjectAsync(RedmineProjectChoice? project, CancellationToken cancellationToken)
    {
        if (project is null)
        {
            throw new InvalidOperationException("プロジェクトを選択してください。");
        }

        RedmineFieldOptionsService service = await GetOrCreateRedmineServiceAsync(requireCredentials: true, cancellationToken)
            ?? throw new InvalidOperationException("Redmine 接続情報を設定してください。");

        byte[] bytes = await service.DownloadIssuesCsvForProjectAsync(project.Identifier, cancellationToken);
        return CsvWorkspaceSession.ImportDownloadedBytes(bytes, $"Redmine Project CSV: {project.Name}", project.Identifier);
    }

    private async Task<RedmineFieldOptionsService?> GetOrCreateRedmineServiceAsync(bool requireCredentials, CancellationToken cancellationToken)
    {
        string baseUrl = txtRedmineUrl.Text.Trim();
        string apiKey = txtApiKey.Text.Trim();

        if (string.IsNullOrWhiteSpace(baseUrl) || string.IsNullOrWhiteSpace(apiKey))
        {
            if (requireCredentials)
            {
                throw new InvalidOperationException("RedmineURL と APIキー を入力してください。");
            }

            _fieldOptionsService?.Dispose();
            _fieldOptionsService = null;
            _serviceBaseUrl = null;
            _serviceApiKey = null;
            BindCreateProjects([]);
            return null;
        }

        if (_fieldOptionsService is not null
            && string.Equals(_serviceBaseUrl, baseUrl, StringComparison.Ordinal)
            && string.Equals(_serviceApiKey, apiKey, StringComparison.Ordinal))
        {
            return _fieldOptionsService;
        }

        RedmineFieldOptionsService service = new(baseUrl, apiKey);
        await service.InitializeAsync(cancellationToken);

        _fieldOptionsService?.Dispose();
        _fieldOptionsService = service;
        _serviceBaseUrl = baseUrl;
        _serviceApiKey = apiKey;
        BindCreateProjects(service.GetProjectChoices());
        return _fieldOptionsService;
    }

    private void BindCsvTable(DataTable table)
    {
        ArgumentNullException.ThrowIfNull(table);

        _csv = table;
        _csvView = new DataView(table);
        gridCsv.DataSource = _csvView;
        ApplyColumnHeaders();
        BuildCsvFilterControls();
        UpdateCsvInfo();
    }

    private async Task RefreshCsvConstraintsAsync(CancellationToken cancellationToken)
    {
        if (_csv is null)
        {
            return;
        }

        RedmineFieldOptionsService? service = await GetOrCreateRedmineServiceAsync(requireCredentials: false, cancellationToken);
        if (service is null)
        {
            AppendLog("[Redmine] RedmineURL と API Key が未入力のため、編集候補の制御はスキップしました。");
            return;
        }

        await ApplyConstraintsToGridAsync(gridCsv, isCreateGrid: false, cancellationToken);
    }

    private async Task RefreshCreateGridConstraintsAsync(CancellationToken cancellationToken)
    {
        if (_createIssuesTable is null)
        {
            return;
        }

        RedmineFieldOptionsService? service = await GetOrCreateRedmineServiceAsync(requireCredentials: false, cancellationToken);
        if (service is null)
        {
            return;
        }

        await ApplyConstraintsToGridAsync(gridCreateIssues, isCreateGrid: true, cancellationToken);
    }

    private async Task ApplyConstraintsToGridAsync(DataGridView grid, bool isCreateGrid, CancellationToken cancellationToken)
    {
        if (_fieldOptionsService is null)
        {
            return;
        }

        SetConstraintFlag(isCreateGrid, true);
        try
        {
            ClearGridConstraints(grid);
            foreach (DataGridViewRow row in grid.Rows)
            {
                if (row.IsNewRow)
                {
                    continue;
                }

                await ApplyConstraintsForRowAsync(grid, row.Index, cancellationToken);
            }
        }
        finally
        {
            SetConstraintFlag(isCreateGrid, false);
        }
    }

    private async Task ApplyConstraintsForRowAsync(DataGridView grid, int rowIndex, CancellationToken cancellationToken)
    {
        if (_fieldOptionsService is null || rowIndex < 0 || rowIndex >= grid.Rows.Count)
        {
            return;
        }

        DataGridViewRow gridRow = grid.Rows[rowIndex];
        if (gridRow.IsNewRow || gridRow.DataBoundItem is not DataRowView rowView)
        {
            return;
        }

        foreach (DataGridViewColumn column in grid.Columns)
        {
            string columnName = GetColumnName(column);
            if (string.IsNullOrWhiteSpace(columnName))
            {
                continue;
            }

            RedmineCellConstraint? constraint = await _fieldOptionsService.GetConstraintAsync(rowView.Row, columnName, cancellationToken);
            if (constraint is null || constraint.AllowsMultiple)
            {
                RestoreTextCell(grid, rowIndex, column.Index);
                continue;
            }

            ApplyComboCell(grid, rowIndex, column.Index, constraint.Options);
        }
    }

    private async Task RefreshCommentsAsync(CancellationToken cancellationToken)
    {
        if (_csv is null)
        {
            BindComments(CreateCommentsTable([]));
            SetCommentsStatus("CSVを読み込むとコメントを表示できます。");
            return;
        }

        List<int> issueIds = GetIssueIds();
        if (issueIds.Count == 0)
        {
            BindComments(CreateCommentsTable([]));
            SetCommentsStatus("CSVにチケット番号がないためコメントを取得できません。");
            return;
        }

        RedmineFieldOptionsService? service = await GetOrCreateRedmineServiceAsync(requireCredentials: false, cancellationToken);
        if (service is null)
        {
            BindComments(CreateCommentsTable([]));
            SetCommentsStatus("Redmine 接続情報を設定するとコメントを取得できます。");
            return;
        }

        UseWaitCursor = true;
        try
        {
            AppendLog($"[Redmine] コメントを取得しています... issues={issueIds.Count}");
            IReadOnlyList<RedmineIssueComment> comments = await service.GetIssueCommentsAsync(issueIds, cancellationToken);
            BindComments(CreateCommentsTable(comments));
            SetCommentsStatus($"コメント {comments.Count} 件を表示しています。既存コメントは参照のみで、追記は『コメント追記』から行えます。");
            AppendLog($"[Redmine] コメントを取得しました: count={comments.Count}");
        }
        finally
        {
            UseWaitCursor = false;
        }
    }

    private void ApplyColumnHeaders()
    {
        if (_csv?.ExtendedProperties["DisplayHeaders"] is not IReadOnlyList<string> displayHeaders)
        {
            return;
        }

        for (int i = 0; i < gridCsv.Columns.Count && i < displayHeaders.Count; i++)
        {
            gridCsv.Columns[i].HeaderText = displayHeaders[i];
        }
    }

    private void BuildCsvFilterControls()
    {
        pnlCsvFilters.SuspendLayout();
        try
        {
            pnlCsvFilters.Controls.Clear();
            _filterInputs.Clear();

            if (_csv is null)
            {
                return;
            }

            foreach (DataColumn column in _csv.Columns)
            {
                Panel panel = new()
                {
                    Width = 160,
                    Height = 48,
                    Margin = new Padding(3)
                };

                Label label = new()
                {
                    AutoSize = false,
                    Width = 152,
                    Height = 15,
                    Location = new System.Drawing.Point(0, 0),
                    Text = column.Caption
                };

                TextBox textBox = new()
                {
                    Width = 152,
                    Location = new System.Drawing.Point(0, 20),
                    Tag = column.ColumnName
                };
                textBox.TextChanged += CsvFilterTextChanged;

                panel.Controls.Add(label);
                panel.Controls.Add(textBox);
                pnlCsvFilters.Controls.Add(panel);
                _filterInputs[column.ColumnName] = textBox;
            }
        }
        finally
        {
            pnlCsvFilters.ResumeLayout();
        }
    }

    private void CsvFilterTextChanged(object? sender, EventArgs e)
    {
        ApplyCsvFilters();
    }

    private void ApplyCsvFilters()
    {
        if (_csvView is null || _csv is null)
        {
            return;
        }

        List<string> conditions = [];
        foreach (KeyValuePair<string, TextBox> filterInput in _filterInputs)
        {
            string filterValue = filterInput.Value.Text.Trim();
            if (string.IsNullOrWhiteSpace(filterValue))
            {
                continue;
            }

            string[] terms = filterValue
                .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(term => !string.IsNullOrWhiteSpace(term))
                .ToArray();

            if (terms.Length == 0)
            {
                continue;
            }

            string[] termConditions = terms
                .Select(term => $"CONVERT({EscapeColumnName(filterInput.Key)}, 'System.String') LIKE '%{EscapeRowFilterValue(term)}%'")
                .ToArray();

            conditions.Add(termConditions.Length == 1
                ? termConditions[0]
                : $"({string.Join(" OR ", termConditions)})");
        }

        _csvView.RowFilter = string.Join(" AND ", conditions);
    }

    private bool SaveCsvToDisk()
    {
        if (_csvSession is null || _csv is null)
        {
            MessageBox.Show("CSVを先に読み込んでください", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return false;
        }

        EndGridEdit(gridCsv);
        _csvSession.Save();
        UpdateCsvInfo();
        AppendLog($"[CSV] Saved: {_csvSession.WorkingFilePath} rows={_csv.Rows.Count}");
        return true;
    }

    private void UpdateCsvInfo()
    {
        lblCsvInfo.Text = _csvSession is null
            ? "未読み込みです。"
            : $"内部CSV: {_csvSession.WorkingFilePath} | source={_csvSession.SourceDescription} | encoding={_csvSession.DetectedEncoding}";
    }

    private void ClearGridConstraints(DataGridView grid)
    {
        foreach (DataGridViewRow row in grid.Rows)
        {
            if (row.IsNewRow)
            {
                continue;
            }

            foreach (DataGridViewColumn column in grid.Columns)
            {
                RestoreTextCell(grid, row.Index, column.Index);
            }
        }
    }

    private void ApplyComboCell(DataGridView grid, int rowIndex, int columnIndex, IReadOnlyList<string> options)
    {
        DataGridViewCell existingCell = grid.Rows[rowIndex].Cells[columnIndex];
        string currentValue = Convert.ToString(existingCell.Value) ?? string.Empty;
        List<string> values = new(options);

        if (!string.IsNullOrWhiteSpace(currentValue) && !values.Contains(currentValue, StringComparer.OrdinalIgnoreCase))
        {
            values.Insert(0, currentValue);
        }

        if (existingCell is DataGridViewComboBoxCell comboCell && ComboCellMatches(comboCell, values))
        {
            return;
        }

        DataGridViewComboBoxCell replacementCell = new()
        {
            DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton,
            FlatStyle = FlatStyle.Flat
        };

        replacementCell.Style = existingCell.Style;
        replacementCell.ReadOnly = existingCell.ReadOnly;
        replacementCell.Items.AddRange(values.Cast<object>().ToArray());
        grid.Rows[rowIndex].Cells[columnIndex] = replacementCell;

        if (!string.IsNullOrWhiteSpace(currentValue))
        {
            replacementCell.Value = currentValue;
        }
    }

    private static void RestoreTextCell(DataGridView grid, int rowIndex, int columnIndex)
    {
        DataGridViewCell existingCell = grid.Rows[rowIndex].Cells[columnIndex];
        if (existingCell is DataGridViewTextBoxCell)
        {
            return;
        }

        DataGridViewTextBoxCell replacementCell = new()
        {
            Style = existingCell.Style,
            ReadOnly = existingCell.ReadOnly,
            Value = existingCell.Value
        };

        grid.Rows[rowIndex].Cells[columnIndex] = replacementCell;
    }

    private void ConfigureCommentsGrid()
    {
        gridComments.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.DisplayedCells;
        gridComments.DefaultCellStyle.WrapMode = DataGridViewTriState.True;
        gridComments.RowHeadersVisible = false;
        gridComments.AllowUserToResizeRows = true;
    }

    private static void ConfigureIssueGrid(DataGridView grid)
    {
        grid.RowHeadersVisible = false;
        grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
    }

    private void BindComments(DataTable table)
    {
        ArgumentNullException.ThrowIfNull(table);

        _commentsTable = table;
        gridComments.DataSource = _commentsTable;

        foreach (string columnName in new[] { "チケット#", "コメントID", "作成者", "作成日時", "コメント" })
        {
            if (_commentsTable.Columns.Contains(columnName))
            {
                _commentsTable.Columns[columnName].ReadOnly = true;
            }
        }

        gridComments.ReadOnly = true;

        if (gridComments.Columns.Contains("コメント"))
        {
            gridComments.Columns["コメント"].Width = 520;
        }
    }

    private void SetCommentsStatus(string text)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(text);
        lblCommentsInfo.Text = text;
    }

    private void BindCreateProjects(IReadOnlyList<RedmineProjectChoice> projects)
    {
        ArgumentNullException.ThrowIfNull(projects);

        List<RedmineProjectChoice> items = [new RedmineProjectChoice(string.Empty, string.Empty)];
        items.AddRange(projects);
        string selectedProjectName = GetSelectedCreateProjectName();
        cmbCreateProject.DataSource = items;

        if (!string.IsNullOrWhiteSpace(selectedProjectName))
        {
            for (int i = 0; i < items.Count; i++)
            {
                if (string.Equals(items[i].Name, selectedProjectName, StringComparison.OrdinalIgnoreCase))
                {
                    cmbCreateProject.SelectedIndex = i;
                    break;
                }
            }
        }
    }

    private void ResetCreateIssueTable()
    {
        DataTable table = new();
        foreach (string columnName in CreateIssueColumns)
        {
            table.Columns.Add(columnName);
        }

        _createIssuesTable = table;
        gridCreateIssues.DataSource = _createIssuesTable;

        string projectName = GetSelectedCreateProjectName();
        if (!string.IsNullOrWhiteSpace(projectName))
        {
            DataRow row = _createIssuesTable.NewRow();
            row["プロジェクト"] = projectName;
            _createIssuesTable.Rows.Add(row);
        }
    }

    private static DataTable CreateCommentsTable(IEnumerable<RedmineIssueComment> comments)
    {
        ArgumentNullException.ThrowIfNull(comments);

        DataTable table = new();
        table.Columns.Add("チケット#", typeof(int));
        table.Columns.Add("コメントID", typeof(int));
        table.Columns.Add("作成者", typeof(string));
        table.Columns.Add("作成日時", typeof(string));
        table.Columns.Add("コメント", typeof(string));

        foreach (RedmineIssueComment comment in comments.OrderBy(item => item.IssueId).ThenBy(item => item.CommentId))
        {
            table.Rows.Add(comment.IssueId, comment.CommentId, comment.AuthorName, comment.CreatedOn, comment.Notes);
        }

        return table;
    }

    private List<int> GetIssueIds()
    {
        if (_csv is null)
        {
            return [];
        }

        string idColumnName = _csv.Columns.Contains("#") ? "#" : _csv.Columns.Contains("﻿#") ? "﻿#" : string.Empty;
        if (string.IsNullOrWhiteSpace(idColumnName))
        {
            return [];
        }

        return _csv.Rows
            .Cast<DataRow>()
            .Select(row => Convert.ToString(row[idColumnName]))
            .Where(value => int.TryParse(value, out _))
            .Select(value => int.Parse(value!))
            .Distinct()
            .ToList();
    }

    private List<IReadOnlyDictionary<string, string>> GetCreateIssueDrafts()
    {
        if (_createIssuesTable is null)
        {
            return [];
        }

        List<IReadOnlyDictionary<string, string>> drafts = [];
        int rowNumber = 1;
        foreach (DataRow row in _createIssuesTable.Rows)
        {
            Dictionary<string, string> values = new(StringComparer.OrdinalIgnoreCase);
            foreach (DataColumn column in _createIssuesTable.Columns)
            {
                values[column.ColumnName] = Convert.ToString(row[column])?.Trim() ?? string.Empty;
            }

            if (values.Values.All(string.IsNullOrWhiteSpace))
            {
                rowNumber++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(values["プロジェクト"]))
            {
                values["プロジェクト"] = GetSelectedCreateProjectName();
            }

            if (string.IsNullOrWhiteSpace(values["プロジェクト"]))
            {
                throw new InvalidOperationException($"作成行 {rowNumber}: プロジェクトは必須です。");
            }

            if (string.IsNullOrWhiteSpace(values["トラッカー"]))
            {
                throw new InvalidOperationException($"作成行 {rowNumber}: トラッカーは必須です。");
            }

            if (string.IsNullOrWhiteSpace(values["題名"]))
            {
                throw new InvalidOperationException($"作成行 {rowNumber}: 題名は必須です。");
            }

            drafts.Add(values);
            rowNumber++;
        }

        return drafts;
    }

    private void AppendCreatedIssuesToCsv(IEnumerable<CreatedIssueInfo> createdIssues)
    {
        if (_csv is null)
        {
            return;
        }

        foreach (CreatedIssueInfo createdIssue in createdIssues)
        {
            DataRow row = _csv.NewRow();
            foreach (DataColumn column in _csv.Columns)
            {
                row[column] = createdIssue.Fields.TryGetValue(column.ColumnName, out string? value) ? value : string.Empty;
            }

            if (_csv.Columns.Contains("#"))
            {
                row["#"] = createdIssue.IssueId.ToString();
            }

            _csv.Rows.Add(row);
        }
    }

    private string GetSelectedCreateProjectName()
        => cmbCreateProject.SelectedItem is RedmineProjectChoice project && !string.IsNullOrWhiteSpace(project.Name)
            ? project.Name
            : string.Empty;

    private static void ClearCreateIssueDependentFields(DataRow row)
    {
        row["トラッカー"] = string.Empty;
        row["ステータス"] = string.Empty;
        row["担当者"] = string.Empty;
        row["カテゴリ"] = string.Empty;
        row["対象バージョン"] = string.Empty;
    }

    private static void HookIssueGridEvents(
        DataGridView grid,
        DataGridViewCellEventHandler cellValueChanged,
        DataGridViewCellValidatingEventHandler cellValidating,
        EventHandler currentCellDirtyStateChanged,
        DataGridViewDataErrorEventHandler dataError)
    {
        grid.DataError += dataError;
        grid.CellValueChanged += cellValueChanged;
        grid.CellValidating += cellValidating;
        grid.CurrentCellDirtyStateChanged += currentCellDirtyStateChanged;
    }

    private static void EndGridEdit(DataGridView grid)
    {
        grid.EndEdit();
        if (grid.DataSource is not null && grid.BindingContext[grid.DataSource] is CurrencyManager currencyManager)
        {
            currencyManager.EndCurrentEdit();
        }
    }

    private static bool ComboCellMatches(DataGridViewComboBoxCell comboCell, IReadOnlyList<string> values)
    {
        if (comboCell.Items.Count != values.Count)
        {
            return false;
        }

        for (int i = 0; i < values.Count; i++)
        {
            if (!string.Equals(Convert.ToString(comboCell.Items[i]), values[i], StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
        }

        return true;
    }

    private static string GetColumnName(DataGridViewColumn column)
        => string.IsNullOrWhiteSpace(column.DataPropertyName) ? column.Name : column.DataPropertyName;

    private static bool IsDependencyColumn(string columnName)
        => columnName is "プロジェクト" or "トラッカー" or "ステータス";

    private static string EscapeColumnName(string value)
        => $"[{value.Replace("]", "]]", StringComparison.Ordinal)}]";

    private static string EscapeRowFilterValue(string value)
        => value
            .Replace("'", "''", StringComparison.Ordinal)
            .Replace("[", "[[]", StringComparison.Ordinal)
            .Replace("]", "]]", StringComparison.Ordinal)
            .Replace("%", "[%]", StringComparison.Ordinal)
            .Replace("*", "[*]", StringComparison.Ordinal);

    private void SetConstraintFlag(bool isCreateGrid, bool value)
    {
        if (isCreateGrid)
        {
            _applyingCreateConstraints = value;
            return;
        }

        _applyingCsvConstraints = value;
    }

    private bool IsApplyingConstraints(bool isCreateGrid)
        => isCreateGrid ? _applyingCreateConstraints : _applyingCsvConstraints;

    private void gridCreateIssues_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right || e.RowIndex < 0)
        {
            return;
        }

        SelectCreateGridRow(e.RowIndex);
    }

    private void gridCsv_CellMouseDown(object? sender, DataGridViewCellMouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right || e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        gridCsv.ClearSelection();
        DataGridViewCell cell = gridCsv.Rows[e.RowIndex].Cells[e.ColumnIndex];
        cell.Selected = true;
        gridCsv.CurrentCell = cell;
    }

    private void cmsCreateRows_Opening(object? sender, CancelEventArgs e)
    {
        List<int> rowIndexes = GetSelectedCreateRowIndexes(includeNewRow: true);
        bool hasRows = rowIndexes.Count > 0;
        bool canCopy = GetSelectedCreateRowIndexes(includeNewRow: false).Count > 0;
        bool canPaste = Clipboard.ContainsText();
        bool canDelete = GetSelectedCreateRowIndexes(includeNewRow: false).Count > 0;

        mnuCreateRowCopy.Enabled = canCopy;
        mnuCreateRowPaste.Enabled = canPaste;
        mnuCreateRowClear.Enabled = hasRows;
        mnuCreateRowDelete.Enabled = canDelete;
        e.Cancel = !hasRows && !canPaste;
    }

    private void cmsIssueCell_Opening(object? sender, CancelEventArgs e)
    {
        DataGridViewCell? cell = gridCsv.CurrentCell;
        bool canAdd = cell is not null
            && cell.RowIndex >= 0
            && cell.ColumnIndex >= 0
            && !string.IsNullOrWhiteSpace(Convert.ToString(cell.Value))
            && _filterInputs.ContainsKey(GetColumnName(gridCsv.Columns[cell.ColumnIndex]));

        mnuIssueAddFilter.Enabled = canAdd;
        e.Cancel = !canAdd;
    }

    private void mnuCreateRowCopy_Click(object? sender, EventArgs e)
    {
        CopySelectedCreateRows();
    }

    private async void mnuCreateRowPaste_Click(object? sender, EventArgs e)
    {
        await PasteCreateRowsAsync();
    }

    private void mnuCreateRowClear_Click(object? sender, EventArgs e)
    {
        ClearSelectedCreateRows();
    }

    private void mnuCreateRowDelete_Click(object? sender, EventArgs e)
    {
        DeleteSelectedCreateRows();
    }

    private void mnuIssueAddFilter_Click(object? sender, EventArgs e)
    {
        AddCurrentIssueCellValueToFilter();
    }

    private async void gridCreateIssues_KeyDown(object? sender, KeyEventArgs e)
    {
        if (gridCreateIssues.IsCurrentCellInEditMode)
        {
            return;
        }

        if (e.Control && e.KeyCode == Keys.C)
        {
            CopySelectedCreateRows();
        }
        else if (e.Control && e.KeyCode == Keys.V)
        {
            await PasteCreateRowsAsync();
        }
        else if (e.KeyCode == Keys.Back)
        {
            ClearSelectedCreateRows();
        }
        else if (e.KeyCode == Keys.Delete)
        {
            DeleteSelectedCreateRows();
        }
        else
        {
            return;
        }

        e.Handled = true;
        e.SuppressKeyPress = true;
    }

    private async void gridCsv_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        await HandleIssueGridCellValueChangedAsync(gridCsv, isCreateGrid: false, e);
    }

    private async void gridCreateIssues_CellValueChanged(object? sender, DataGridViewCellEventArgs e)
    {
        await HandleIssueGridCellValueChangedAsync(gridCreateIssues, isCreateGrid: true, e);
    }

    private async Task HandleIssueGridCellValueChangedAsync(DataGridView grid, bool isCreateGrid, DataGridViewCellEventArgs e)
    {
        if (IsApplyingConstraints(isCreateGrid) || _fieldOptionsService is null || e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        string columnName = GetColumnName(grid.Columns[e.ColumnIndex]);
        if (!IsDependencyColumn(columnName))
        {
            return;
        }

        try
        {
            await ApplyConstraintsForRowAsync(grid, e.RowIndex, CancellationToken.None);
        }
        catch (Exception ex)
        {
            AppendLog($"[Redmine] 編集候補の再適用に失敗しました: {ex.Message}");
        }
    }

    private void gridCsv_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
    {
        ValidateIssueGridCell(gridCsv, e);
    }

    private void gridCreateIssues_CellValidating(object? sender, DataGridViewCellValidatingEventArgs e)
    {
        ValidateIssueGridCell(gridCreateIssues, e);
    }

    private void ValidateIssueGridCell(DataGridView grid, DataGridViewCellValidatingEventArgs e)
    {
        if (_fieldOptionsService is null || e.RowIndex < 0 || e.ColumnIndex < 0)
        {
            return;
        }

        if (grid.Rows[e.RowIndex].DataBoundItem is not DataRowView rowView)
        {
            return;
        }

        try
        {
            string columnName = GetColumnName(grid.Columns[e.ColumnIndex]);
            RedmineCellConstraint? constraint = _fieldOptionsService.GetConstraintAsync(rowView.Row, columnName, CancellationToken.None).GetAwaiter().GetResult();
            if (constraint is null || !constraint.AllowsMultiple)
            {
                grid.Rows[e.RowIndex].ErrorText = string.Empty;
                return;
            }

            IReadOnlyList<string> selections = RedmineFieldOptionsService.SplitSelections(Convert.ToString(e.FormattedValue));
            string[] invalidSelections = selections
                .Where(selection => !constraint.Options.Contains(selection, StringComparer.OrdinalIgnoreCase))
                .ToArray();

            if (invalidSelections.Length == 0)
            {
                grid.Rows[e.RowIndex].ErrorText = string.Empty;
                return;
            }

            e.Cancel = true;
            grid.Rows[e.RowIndex].ErrorText = $"選択できない値が含まれています: {string.Join(", ", invalidSelections)}";
        }
        catch (Exception ex)
        {
            AppendLog($"[Redmine] 編集値の検証に失敗しました: {ex.Message}");
        }
    }

    private void gridCsv_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        CommitComboCellEdit(gridCsv);
    }

    private void gridCreateIssues_CurrentCellDirtyStateChanged(object? sender, EventArgs e)
    {
        CommitComboCellEdit(gridCreateIssues);
    }

    private static void CommitComboCellEdit(DataGridView grid)
    {
        if (grid.IsCurrentCellDirty && grid.CurrentCell is DataGridViewComboBoxCell)
        {
            grid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private void gridCsv_DataError(object? sender, DataGridViewDataErrorEventArgs e)
    {
        e.ThrowException = false;
    }

    private void gridCreateIssues_DataError(object? sender, DataGridViewDataErrorEventArgs e)
    {
        e.ThrowException = false;
    }

    private void CopySelectedCreateRows()
    {
        if (_createIssuesTable is null)
        {
            return;
        }

        List<int> rowIndexes = GetSelectedCreateRowIndexes(includeNewRow: false);
        if (rowIndexes.Count == 0)
        {
            return;
        }

        string[] lines = rowIndexes
            .Select(rowIndex => string.Join("\t", CreateIssueColumns.Select(columnName => Convert.ToString(_createIssuesTable.Rows[rowIndex][columnName]) ?? string.Empty)))
            .ToArray();

        Clipboard.SetText(string.Join(Environment.NewLine, lines));
    }

    private async Task PasteCreateRowsAsync()
    {
        if (_createIssuesTable is null || !Clipboard.ContainsText())
        {
            return;
        }

        string text = Clipboard.GetText();
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        string[] lines = text
            .Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToArray();

        if (lines.Length == 0)
        {
            return;
        }

        int startRowIndex = GetCreatePasteStartRowIndex();
        for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
        {
            int targetRowIndex = startRowIndex + lineIndex;
            EnsureCreateRowExists(targetRowIndex);

            string[] values = lines[lineIndex].Split('\t');
            DataRow row = _createIssuesTable.Rows[targetRowIndex];
            for (int columnIndex = 0; columnIndex < CreateIssueColumns.Length; columnIndex++)
            {
                row[CreateIssueColumns[columnIndex]] = columnIndex < values.Length ? values[columnIndex] : string.Empty;
            }

            if (string.IsNullOrWhiteSpace(Convert.ToString(row["プロジェクト"])) && !string.IsNullOrWhiteSpace(GetSelectedCreateProjectName()))
            {
                row["プロジェクト"] = GetSelectedCreateProjectName();
            }
        }

        await RefreshCreateGridConstraintsAsync(CancellationToken.None);
    }

    private void ClearSelectedCreateRows()
    {
        if (_createIssuesTable is null)
        {
            return;
        }

        List<int> rowIndexes = GetSelectedCreateRowIndexes(includeNewRow: true);
        foreach (int rowIndex in rowIndexes)
        {
            if (rowIndex >= _createIssuesTable.Rows.Count)
            {
                continue;
            }

            DataRow row = _createIssuesTable.Rows[rowIndex];
            foreach (string columnName in CreateIssueColumns)
            {
                row[columnName] = string.Empty;
            }
        }
    }

    private void DeleteSelectedCreateRows()
    {
        if (_createIssuesTable is null)
        {
            return;
        }

        foreach (int rowIndex in GetSelectedCreateRowIndexes(includeNewRow: false).OrderByDescending(index => index))
        {
            _createIssuesTable.Rows.RemoveAt(rowIndex);
        }
    }

    private void AddCurrentIssueCellValueToFilter()
    {
        DataGridViewCell? cell = gridCsv.CurrentCell;
        if (cell is null || cell.RowIndex < 0 || cell.ColumnIndex < 0)
        {
            return;
        }

        string value = Convert.ToString(cell.Value)?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        string columnName = GetColumnName(gridCsv.Columns[cell.ColumnIndex]);
        if (!_filterInputs.TryGetValue(columnName, out TextBox? filterTextBox))
        {
            return;
        }

        string[] existingTerms = filterTextBox.Text
            .Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (existingTerms.Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            return;
        }

        filterTextBox.Text = string.IsNullOrWhiteSpace(filterTextBox.Text)
            ? value
            : $"{filterTextBox.Text} | {value}";
    }

    private void SelectCreateGridRow(int rowIndex)
    {
        if (rowIndex < 0 || rowIndex >= gridCreateIssues.Rows.Count)
        {
            return;
        }

        if (!gridCreateIssues.Rows[rowIndex].Selected)
        {
            gridCreateIssues.ClearSelection();
            gridCreateIssues.Rows[rowIndex].Selected = true;
        }

        if (gridCreateIssues.Columns.Count > 0)
        {
            gridCreateIssues.CurrentCell = gridCreateIssues.Rows[rowIndex].Cells[0];
        }
    }

    private List<int> GetSelectedCreateRowIndexes(bool includeNewRow)
    {
        List<int> rowIndexes = gridCreateIssues.SelectedRows
            .Cast<DataGridViewRow>()
            .Select(row => row.Index)
            .Concat(gridCreateIssues.SelectedCells.Cast<DataGridViewCell>().Select(cell => cell.RowIndex))
            .Distinct()
            .Where(rowIndex => rowIndex >= 0)
            .ToList();

        if (rowIndexes.Count == 0 && gridCreateIssues.CurrentCell is not null)
        {
            rowIndexes.Add(gridCreateIssues.CurrentCell.RowIndex);
        }

        return rowIndexes
            .Where(rowIndex => includeNewRow || rowIndex < (_createIssuesTable?.Rows.Count ?? 0))
            .Distinct()
            .OrderBy(index => index)
            .ToList();
    }

    private int GetCreatePasteStartRowIndex()
    {
        if (_createIssuesTable is null)
        {
            return 0;
        }

        if (gridCreateIssues.CurrentCell is null)
        {
            return _createIssuesTable.Rows.Count;
        }

        int rowIndex = gridCreateIssues.CurrentCell.RowIndex;
        return rowIndex >= _createIssuesTable.Rows.Count ? _createIssuesTable.Rows.Count : rowIndex;
    }

    private void EnsureCreateRowExists(int rowIndex)
    {
        if (_createIssuesTable is null)
        {
            return;
        }

        while (_createIssuesTable.Rows.Count <= rowIndex)
        {
            DataRow row = _createIssuesTable.NewRow();
            if (!string.IsNullOrWhiteSpace(GetSelectedCreateProjectName()))
            {
                row["プロジェクト"] = GetSelectedCreateProjectName();
            }

            _createIssuesTable.Rows.Add(row);
        }
    }
}