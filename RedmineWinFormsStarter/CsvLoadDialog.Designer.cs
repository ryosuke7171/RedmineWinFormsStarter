namespace RedmineWinFormsStarter;

partial class CsvLoadDialog
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.RadioButton rdoLocalFile;
    private System.Windows.Forms.TextBox txtLocalPath;
    private System.Windows.Forms.Button btnBrowseLocal;
    private System.Windows.Forms.RadioButton rdoUrlDownload;
    private System.Windows.Forms.TextBox txtDownloadUrl;
    private System.Windows.Forms.RadioButton rdoProjectDownload;
    private System.Windows.Forms.ComboBox cmbProjects;
    private System.Windows.Forms.Button btnOk;
    private System.Windows.Forms.Button btnCancel;
    private System.Windows.Forms.Label lblGuide;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.rdoLocalFile = new System.Windows.Forms.RadioButton();
        this.txtLocalPath = new System.Windows.Forms.TextBox();
        this.btnBrowseLocal = new System.Windows.Forms.Button();
        this.rdoUrlDownload = new System.Windows.Forms.RadioButton();
        this.txtDownloadUrl = new System.Windows.Forms.TextBox();
        this.rdoProjectDownload = new System.Windows.Forms.RadioButton();
        this.cmbProjects = new System.Windows.Forms.ComboBox();
        this.btnOk = new System.Windows.Forms.Button();
        this.btnCancel = new System.Windows.Forms.Button();
        this.lblGuide = new System.Windows.Forms.Label();
        this.SuspendLayout();

        this.lblGuide.AutoSize = true;
        this.lblGuide.Location = new System.Drawing.Point(12, 12);
        this.lblGuide.Name = "lblGuide";
        this.lblGuide.Size = new System.Drawing.Size(309, 15);
        this.lblGuide.Text = "CSV の読み込み方法を選択してください。ダウンロード結果は内部管理されます。";

        this.rdoLocalFile.AutoSize = true;
        this.rdoLocalFile.Checked = true;
        this.rdoLocalFile.Location = new System.Drawing.Point(12, 42);
        this.rdoLocalFile.Name = "rdoLocalFile";
        this.rdoLocalFile.Size = new System.Drawing.Size(153, 19);
        this.rdoLocalFile.TabStop = true;
        this.rdoLocalFile.Text = "ローカルのCSVを取り込む";
        this.rdoLocalFile.UseVisualStyleBackColor = true;
        this.rdoLocalFile.CheckedChanged += new System.EventHandler(this.ModeChanged);

        this.txtLocalPath.Location = new System.Drawing.Point(32, 67);
        this.txtLocalPath.Name = "txtLocalPath";
        this.txtLocalPath.Size = new System.Drawing.Size(380, 23);

        this.btnBrowseLocal.Location = new System.Drawing.Point(418, 66);
        this.btnBrowseLocal.Name = "btnBrowseLocal";
        this.btnBrowseLocal.Size = new System.Drawing.Size(75, 25);
        this.btnBrowseLocal.Text = "参照";
        this.btnBrowseLocal.UseVisualStyleBackColor = true;
        this.btnBrowseLocal.Click += new System.EventHandler(this.btnBrowseLocal_Click);

        this.rdoUrlDownload.AutoSize = true;
        this.rdoUrlDownload.Location = new System.Drawing.Point(12, 108);
        this.rdoUrlDownload.Name = "rdoUrlDownload";
        this.rdoUrlDownload.Size = new System.Drawing.Size(195, 19);
        this.rdoUrlDownload.Text = "RedmineからCSVを直接ダウンロード";
        this.rdoUrlDownload.UseVisualStyleBackColor = true;
        this.rdoUrlDownload.CheckedChanged += new System.EventHandler(this.ModeChanged);

        this.txtDownloadUrl.Location = new System.Drawing.Point(32, 133);
        this.txtDownloadUrl.Name = "txtDownloadUrl";
        this.txtDownloadUrl.Size = new System.Drawing.Size(461, 23);

        this.rdoProjectDownload.AutoSize = true;
        this.rdoProjectDownload.Location = new System.Drawing.Point(12, 174);
        this.rdoProjectDownload.Name = "rdoProjectDownload";
        this.rdoProjectDownload.Size = new System.Drawing.Size(211, 19);
        this.rdoProjectDownload.Text = "プロジェクト指定でCSVを直接ダウンロード";
        this.rdoProjectDownload.UseVisualStyleBackColor = true;
        this.rdoProjectDownload.CheckedChanged += new System.EventHandler(this.ModeChanged);

        this.cmbProjects.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
        this.cmbProjects.FormattingEnabled = true;
        this.cmbProjects.Location = new System.Drawing.Point(32, 199);
        this.cmbProjects.Name = "cmbProjects";
        this.cmbProjects.Size = new System.Drawing.Size(461, 23);

        this.btnOk.Location = new System.Drawing.Point(337, 244);
        this.btnOk.Name = "btnOk";
        this.btnOk.Size = new System.Drawing.Size(75, 27);
        this.btnOk.Text = "決定";
        this.btnOk.UseVisualStyleBackColor = true;
        this.btnOk.Click += new System.EventHandler(this.btnOk_Click);

        this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnCancel.Location = new System.Drawing.Point(418, 244);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(75, 27);
        this.btnCancel.Text = "キャンセル";
        this.btnCancel.UseVisualStyleBackColor = true;

        this.AcceptButton = this.btnOk;
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.CancelButton = this.btnCancel;
        this.ClientSize = new System.Drawing.Size(508, 286);
        this.Controls.Add(this.lblGuide);
        this.Controls.Add(this.rdoLocalFile);
        this.Controls.Add(this.txtLocalPath);
        this.Controls.Add(this.btnBrowseLocal);
        this.Controls.Add(this.rdoUrlDownload);
        this.Controls.Add(this.txtDownloadUrl);
        this.Controls.Add(this.rdoProjectDownload);
        this.Controls.Add(this.cmbProjects);
        this.Controls.Add(this.btnOk);
        this.Controls.Add(this.btnCancel);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "CsvLoadDialog";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "CSV読込";
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}