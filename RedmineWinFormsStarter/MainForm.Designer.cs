namespace RedmineWinFormsStarter;

partial class MainForm
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.TextBox txtRedmineUrl;
    private System.Windows.Forms.TextBox txtApiKey;
    private System.Windows.Forms.Button btnDryRun;
    private System.Windows.Forms.Button btnUpdate;
    private System.Windows.Forms.Button btnLoadCsv;
    private System.Windows.Forms.Button btnSaveCsv;
    private System.Windows.Forms.DataGridView gridCsv;
    private System.Windows.Forms.DataGridView gridComments;
    private System.Windows.Forms.DataGridView gridCreateIssues;
    private System.Windows.Forms.TextBox txtLog;
    private System.Windows.Forms.Label lblUrl;
    private System.Windows.Forms.Label lblKey;
    private System.Windows.Forms.TabControl tabMain;
    private System.Windows.Forms.TabPage tabCsv;
    private System.Windows.Forms.TabPage tabComments;
    private System.Windows.Forms.TabPage tabCreateIssues;
    private System.Windows.Forms.Label lblCsvInfo;
    private System.Windows.Forms.Button btnRefreshComments;
    private System.Windows.Forms.Button btnApplyCommentUpdates;
    private System.Windows.Forms.Label lblCommentsInfo;
    private System.Windows.Forms.FlowLayoutPanel pnlCsvFilters;
    private System.Windows.Forms.Button btnClearCsvFilters;
    private System.Windows.Forms.ComboBox cmbCreateProject;
    private System.Windows.Forms.Label lblCreateProject;
    private System.Windows.Forms.Label lblCreateInfo;
    private System.Windows.Forms.Button btnCreateIssues;
    private System.Windows.Forms.Button btnResetCreateIssues;
    private System.Windows.Forms.ContextMenuStrip cmsCreateRows;
    private System.Windows.Forms.ToolStripMenuItem mnuCreateRowCopy;
    private System.Windows.Forms.ToolStripMenuItem mnuCreateRowPaste;
    private System.Windows.Forms.ToolStripMenuItem mnuCreateRowClear;
    private System.Windows.Forms.ToolStripMenuItem mnuCreateRowDelete;
    private System.Windows.Forms.ContextMenuStrip cmsIssueCell;
    private System.Windows.Forms.ToolStripMenuItem mnuIssueAddFilter;

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _fieldOptionsService?.Dispose();
            if (components != null) components.Dispose();
        }
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.components = new System.ComponentModel.Container();
        this.txtRedmineUrl = new System.Windows.Forms.TextBox();
        this.txtApiKey = new System.Windows.Forms.TextBox();
        this.btnDryRun = new System.Windows.Forms.Button();
        this.btnUpdate = new System.Windows.Forms.Button();
        this.btnLoadCsv = new System.Windows.Forms.Button();
        this.btnSaveCsv = new System.Windows.Forms.Button();
        this.gridCsv = new System.Windows.Forms.DataGridView();
        this.gridComments = new System.Windows.Forms.DataGridView();
        this.gridCreateIssues = new System.Windows.Forms.DataGridView();
        this.txtLog = new System.Windows.Forms.TextBox();
        this.lblUrl = new System.Windows.Forms.Label();
        this.lblKey = new System.Windows.Forms.Label();
        this.tabMain = new System.Windows.Forms.TabControl();
        this.tabCsv = new System.Windows.Forms.TabPage();
        this.tabComments = new System.Windows.Forms.TabPage();
        this.tabCreateIssues = new System.Windows.Forms.TabPage();
        this.lblCsvInfo = new System.Windows.Forms.Label();
        this.btnRefreshComments = new System.Windows.Forms.Button();
        this.btnApplyCommentUpdates = new System.Windows.Forms.Button();
        this.lblCommentsInfo = new System.Windows.Forms.Label();
        this.pnlCsvFilters = new System.Windows.Forms.FlowLayoutPanel();
        this.btnClearCsvFilters = new System.Windows.Forms.Button();
        this.cmbCreateProject = new System.Windows.Forms.ComboBox();
        this.lblCreateProject = new System.Windows.Forms.Label();
        this.lblCreateInfo = new System.Windows.Forms.Label();
        this.btnCreateIssues = new System.Windows.Forms.Button();
        this.btnResetCreateIssues = new System.Windows.Forms.Button();
        this.cmsCreateRows = new System.Windows.Forms.ContextMenuStrip(this.components);
        this.mnuCreateRowCopy = new System.Windows.Forms.ToolStripMenuItem();
        this.mnuCreateRowPaste = new System.Windows.Forms.ToolStripMenuItem();
        this.mnuCreateRowClear = new System.Windows.Forms.ToolStripMenuItem();
        this.mnuCreateRowDelete = new System.Windows.Forms.ToolStripMenuItem();
        this.cmsIssueCell = new System.Windows.Forms.ContextMenuStrip(this.components);
        this.mnuIssueAddFilter = new System.Windows.Forms.ToolStripMenuItem();
        ((System.ComponentModel.ISupportInitialize)(this.gridCsv)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.gridComments)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.gridCreateIssues)).BeginInit();
        this.tabMain.SuspendLayout();
        this.tabCsv.SuspendLayout();
        this.tabComments.SuspendLayout();
        this.tabCreateIssues.SuspendLayout();
        this.cmsCreateRows.SuspendLayout();
        this.cmsIssueCell.SuspendLayout();
        this.SuspendLayout();

        this.lblUrl.AutoSize = true;
        this.lblUrl.Location = new System.Drawing.Point(12, 15);
        this.lblUrl.Name = "lblUrl";
        this.lblUrl.Size = new System.Drawing.Size(74, 15);
        this.lblUrl.Text = "RedmineURL";

        this.txtRedmineUrl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
        this.txtRedmineUrl.Location = new System.Drawing.Point(100, 12);
        this.txtRedmineUrl.Name = "txtRedmineUrl";
        this.txtRedmineUrl.Size = new System.Drawing.Size(640, 23);

        this.lblKey.AutoSize = true;
        this.lblKey.Location = new System.Drawing.Point(12, 45);
        this.lblKey.Name = "lblKey";
        this.lblKey.Size = new System.Drawing.Size(45, 15);
        this.lblKey.Text = "API Key";

        this.txtApiKey.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
        this.txtApiKey.Location = new System.Drawing.Point(100, 42);
        this.txtApiKey.Name = "txtApiKey";
        this.txtApiKey.Size = new System.Drawing.Size(640, 23);
        this.txtApiKey.UseSystemPasswordChar = true;

        this.btnDryRun.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnDryRun.Location = new System.Drawing.Point(760, 10);
        this.btnDryRun.Name = "btnDryRun";
        this.btnDryRun.Size = new System.Drawing.Size(120, 25);
        this.btnDryRun.Text = "検証実行";
        this.btnDryRun.UseVisualStyleBackColor = true;
        this.btnDryRun.Click += new System.EventHandler(this.btnDryRun_Click);

        this.btnUpdate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnUpdate.Location = new System.Drawing.Point(760, 40);
        this.btnUpdate.Name = "btnUpdate";
        this.btnUpdate.Size = new System.Drawing.Size(120, 25);
        this.btnUpdate.Text = "Redmine更新";
        this.btnUpdate.UseVisualStyleBackColor = true;
        this.btnUpdate.Click += new System.EventHandler(this.btnUpdate_Click);

        this.btnLoadCsv.Location = new System.Drawing.Point(12, 75);
        this.btnLoadCsv.Name = "btnLoadCsv";
        this.btnLoadCsv.Size = new System.Drawing.Size(120, 25);
        this.btnLoadCsv.Text = "CSV読込";
        this.btnLoadCsv.UseVisualStyleBackColor = true;
        this.btnLoadCsv.Click += new System.EventHandler(this.btnLoadCsv_Click);

        this.btnSaveCsv.Location = new System.Drawing.Point(138, 75);
        this.btnSaveCsv.Name = "btnSaveCsv";
        this.btnSaveCsv.Size = new System.Drawing.Size(120, 25);
        this.btnSaveCsv.Text = "CSV保存";
        this.btnSaveCsv.UseVisualStyleBackColor = true;
        this.btnSaveCsv.Click += new System.EventHandler(this.btnSaveCsv_Click);

        this.tabMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
        this.tabMain.Controls.Add(this.tabCsv);
        this.tabMain.Controls.Add(this.tabComments);
        this.tabMain.Controls.Add(this.tabCreateIssues);
        this.tabMain.Location = new System.Drawing.Point(12, 110);
        this.tabMain.Name = "tabMain";
        this.tabMain.SelectedIndex = 0;
        this.tabMain.Size = new System.Drawing.Size(868, 330);

        this.tabCsv.Controls.Add(this.btnClearCsvFilters);
        this.tabCsv.Controls.Add(this.pnlCsvFilters);
        this.tabCsv.Controls.Add(this.gridCsv);
        this.tabCsv.Controls.Add(this.lblCsvInfo);
        this.tabCsv.Location = new System.Drawing.Point(4, 24);
        this.tabCsv.Name = "tabCsv";
        this.tabCsv.Padding = new System.Windows.Forms.Padding(3);
        this.tabCsv.Size = new System.Drawing.Size(860, 302);
        this.tabCsv.Text = "チケット";
        this.tabCsv.UseVisualStyleBackColor = true;

        this.btnClearCsvFilters.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnClearCsvFilters.Location = new System.Drawing.Point(742, 6);
        this.btnClearCsvFilters.Name = "btnClearCsvFilters";
        this.btnClearCsvFilters.Size = new System.Drawing.Size(112, 25);
        this.btnClearCsvFilters.Text = "フィルタ解除";
        this.btnClearCsvFilters.UseVisualStyleBackColor = true;
        this.btnClearCsvFilters.Click += new System.EventHandler(this.btnClearCsvFilters_Click);

        this.pnlCsvFilters.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) | System.Windows.Forms.AnchorStyles.Right)));
        this.pnlCsvFilters.AutoScroll = true;
        this.pnlCsvFilters.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        this.pnlCsvFilters.Location = new System.Drawing.Point(6, 6);
        this.pnlCsvFilters.Name = "pnlCsvFilters";
        this.pnlCsvFilters.Size = new System.Drawing.Size(730, 58);

        this.gridCsv.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
        this.gridCsv.Location = new System.Drawing.Point(6, 70);
        this.gridCsv.Name = "gridCsv";
        this.gridCsv.Size = new System.Drawing.Size(848, 198);
        this.gridCsv.AllowUserToAddRows = true;
        this.gridCsv.AllowUserToDeleteRows = true;

        this.lblCsvInfo.AutoSize = true;
        this.lblCsvInfo.Location = new System.Drawing.Point(6, 277);
        this.lblCsvInfo.Name = "lblCsvInfo";
        this.lblCsvInfo.Size = new System.Drawing.Size(109, 15);
        this.lblCsvInfo.Text = "未読み込みです。";

        this.tabComments.Controls.Add(this.btnRefreshComments);
        this.tabComments.Controls.Add(this.btnApplyCommentUpdates);
        this.tabComments.Controls.Add(this.lblCommentsInfo);
        this.tabComments.Controls.Add(this.gridComments);
        this.tabComments.Location = new System.Drawing.Point(4, 24);
        this.tabComments.Name = "tabComments";
        this.tabComments.Padding = new System.Windows.Forms.Padding(3);
        this.tabComments.Size = new System.Drawing.Size(860, 302);
        this.tabComments.Text = "コメント";
        this.tabComments.UseVisualStyleBackColor = true;

        this.btnRefreshComments.Location = new System.Drawing.Point(6, 6);
        this.btnRefreshComments.Name = "btnRefreshComments";
        this.btnRefreshComments.Size = new System.Drawing.Size(120, 25);
        this.btnRefreshComments.Text = "コメント再読込";
        this.btnRefreshComments.UseVisualStyleBackColor = true;
        this.btnRefreshComments.Click += new System.EventHandler(this.btnRefreshComments_Click);

        this.btnApplyCommentUpdates.Location = new System.Drawing.Point(132, 6);
        this.btnApplyCommentUpdates.Name = "btnApplyCommentUpdates";
        this.btnApplyCommentUpdates.Size = new System.Drawing.Size(160, 25);
        this.btnApplyCommentUpdates.Text = "コメント追記";
        this.btnApplyCommentUpdates.UseVisualStyleBackColor = true;
        this.btnApplyCommentUpdates.Click += new System.EventHandler(this.btnApplyCommentUpdates_Click);

        this.lblCommentsInfo.AutoSize = true;
        this.lblCommentsInfo.Location = new System.Drawing.Point(308, 11);
        this.lblCommentsInfo.Name = "lblCommentsInfo";
        this.lblCommentsInfo.Size = new System.Drawing.Size(384, 15);
        this.lblCommentsInfo.Text = "既存コメントは参照のみです。コメント追記から新しいコメントを追加してください。";

        this.gridComments.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
        this.gridComments.Location = new System.Drawing.Point(6, 37);
        this.gridComments.Name = "gridComments";
        this.gridComments.Size = new System.Drawing.Size(848, 259);
        this.gridComments.AllowUserToAddRows = false;
        this.gridComments.AllowUserToDeleteRows = false;

        this.tabCreateIssues.Controls.Add(this.btnResetCreateIssues);
        this.tabCreateIssues.Controls.Add(this.btnCreateIssues);
        this.tabCreateIssues.Controls.Add(this.lblCreateInfo);
        this.tabCreateIssues.Controls.Add(this.lblCreateProject);
        this.tabCreateIssues.Controls.Add(this.cmbCreateProject);
        this.tabCreateIssues.Controls.Add(this.gridCreateIssues);
        this.tabCreateIssues.Location = new System.Drawing.Point(4, 24);
        this.tabCreateIssues.Name = "tabCreateIssues";
        this.tabCreateIssues.Padding = new System.Windows.Forms.Padding(3);
        this.tabCreateIssues.Size = new System.Drawing.Size(860, 302);
        this.tabCreateIssues.Text = "新規追加";
        this.tabCreateIssues.UseVisualStyleBackColor = true;

        this.lblCreateProject.AutoSize = true;
        this.lblCreateProject.Location = new System.Drawing.Point(6, 11);
        this.lblCreateProject.Name = "lblCreateProject";
        this.lblCreateProject.Size = new System.Drawing.Size(63, 15);
        this.lblCreateProject.Text = "プロジェクト";

        this.cmbCreateProject.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cmbCreateProject.FormattingEnabled = true;
        this.cmbCreateProject.Location = new System.Drawing.Point(75, 8);
        this.cmbCreateProject.Name = "cmbCreateProject";
        this.cmbCreateProject.Size = new System.Drawing.Size(260, 23);
        this.cmbCreateProject.SelectedIndexChanged += new System.EventHandler(this.cmbCreateProject_SelectedIndexChanged);

        this.btnCreateIssues.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnCreateIssues.Location = new System.Drawing.Point(734, 6);
        this.btnCreateIssues.Name = "btnCreateIssues";
        this.btnCreateIssues.Size = new System.Drawing.Size(120, 25);
        this.btnCreateIssues.Text = "チケット作成";
        this.btnCreateIssues.UseVisualStyleBackColor = true;
        this.btnCreateIssues.Click += new System.EventHandler(this.btnCreateIssues_Click);

        this.btnResetCreateIssues.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.btnResetCreateIssues.Location = new System.Drawing.Point(608, 6);
        this.btnResetCreateIssues.Name = "btnResetCreateIssues";
        this.btnResetCreateIssues.Size = new System.Drawing.Size(120, 25);
        this.btnResetCreateIssues.Text = "入力クリア";
        this.btnResetCreateIssues.UseVisualStyleBackColor = true;
        this.btnResetCreateIssues.Click += new System.EventHandler(this.btnResetCreateIssues_Click);

        this.lblCreateInfo.AutoSize = true;
        this.lblCreateInfo.Location = new System.Drawing.Point(341, 11);
        this.lblCreateInfo.Name = "lblCreateInfo";
        this.lblCreateInfo.Size = new System.Drawing.Size(240, 15);
        this.lblCreateInfo.Text = "複数行を入力して一括作成できます。空行は無視されます。";

        this.gridCreateIssues.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
        this.gridCreateIssues.Location = new System.Drawing.Point(6, 37);
        this.gridCreateIssues.Name = "gridCreateIssues";
        this.gridCreateIssues.Size = new System.Drawing.Size(848, 259);
        this.gridCreateIssues.AllowUserToAddRows = true;
        this.gridCreateIssues.AllowUserToDeleteRows = true;
        this.gridCreateIssues.ContextMenuStrip = this.cmsCreateRows;

        this.cmsCreateRows.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuCreateRowCopy,
            this.mnuCreateRowPaste,
            this.mnuCreateRowClear,
            this.mnuCreateRowDelete});
        this.cmsCreateRows.Name = "cmsCreateRows";
        this.cmsCreateRows.Size = new System.Drawing.Size(181, 114);

        this.mnuCreateRowCopy.Name = "mnuCreateRowCopy";
        this.mnuCreateRowCopy.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.C)));
        this.mnuCreateRowCopy.Size = new System.Drawing.Size(180, 22);
        this.mnuCreateRowCopy.Text = "行の内容のコピー";
        this.mnuCreateRowCopy.Click += new System.EventHandler(this.mnuCreateRowCopy_Click);

        this.mnuCreateRowPaste.Name = "mnuCreateRowPaste";
        this.mnuCreateRowPaste.ShortcutKeys = ((System.Windows.Forms.Keys)((System.Windows.Forms.Keys.Control | System.Windows.Forms.Keys.V)));
        this.mnuCreateRowPaste.Size = new System.Drawing.Size(180, 22);
        this.mnuCreateRowPaste.Text = "行の内容のペースト";
        this.mnuCreateRowPaste.Click += new System.EventHandler(this.mnuCreateRowPaste_Click);

        this.mnuCreateRowClear.Name = "mnuCreateRowClear";
        this.mnuCreateRowClear.ShortcutKeyDisplayString = "Backspace";
        this.mnuCreateRowClear.Size = new System.Drawing.Size(180, 22);
        this.mnuCreateRowClear.Text = "行の内容のクリア";
        this.mnuCreateRowClear.Click += new System.EventHandler(this.mnuCreateRowClear_Click);

        this.mnuCreateRowDelete.Name = "mnuCreateRowDelete";
        this.mnuCreateRowDelete.ShortcutKeys = System.Windows.Forms.Keys.Delete;
        this.mnuCreateRowDelete.Size = new System.Drawing.Size(180, 22);
        this.mnuCreateRowDelete.Text = "行自体の削除";
        this.mnuCreateRowDelete.Click += new System.EventHandler(this.mnuCreateRowDelete_Click);

        this.cmsIssueCell.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuIssueAddFilter});
        this.cmsIssueCell.Name = "cmsIssueCell";
        this.cmsIssueCell.Size = new System.Drawing.Size(210, 26);

        this.mnuIssueAddFilter.Name = "mnuIssueAddFilter";
        this.mnuIssueAddFilter.Size = new System.Drawing.Size(209, 22);
        this.mnuIssueAddFilter.Text = "この値をフィルターに追加";
        this.mnuIssueAddFilter.Click += new System.EventHandler(this.mnuIssueAddFilter_Click);

        this.gridCsv.ContextMenuStrip = this.cmsIssueCell;

        this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
        this.txtLog.Location = new System.Drawing.Point(12, 446);
        this.txtLog.Multiline = true;
        this.txtLog.Name = "txtLog";
        this.txtLog.ReadOnly = true;
        this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.txtLog.Size = new System.Drawing.Size(868, 134);

        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(892, 592);
        this.Controls.Add(this.lblUrl);
        this.Controls.Add(this.txtRedmineUrl);
        this.Controls.Add(this.lblKey);
        this.Controls.Add(this.txtApiKey);
        this.Controls.Add(this.btnDryRun);
        this.Controls.Add(this.btnUpdate);
        this.Controls.Add(this.btnLoadCsv);
        this.Controls.Add(this.btnSaveCsv);
        this.Controls.Add(this.tabMain);
        this.Controls.Add(this.txtLog);
        this.Name = "MainForm";
        this.Text = "Redmine WinForms Starter (.NET 10)";
        ((System.ComponentModel.ISupportInitialize)(this.gridCsv)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.gridComments)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.gridCreateIssues)).EndInit();
        this.tabMain.ResumeLayout(false);
        this.tabCsv.ResumeLayout(false);
        this.tabCsv.PerformLayout();
        this.tabComments.ResumeLayout(false);
        this.tabComments.PerformLayout();
        this.tabCreateIssues.ResumeLayout(false);
        this.tabCreateIssues.PerformLayout();
        this.cmsCreateRows.ResumeLayout(false);
        this.cmsIssueCell.ResumeLayout(false);
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}
