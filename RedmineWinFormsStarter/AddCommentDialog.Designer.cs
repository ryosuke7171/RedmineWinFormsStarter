namespace RedmineWinFormsStarter;

partial class AddCommentDialog
{
    private System.ComponentModel.IContainer components = null;
    private System.Windows.Forms.Label lblIssueId;
    private System.Windows.Forms.TextBox txtIssueId;
    private System.Windows.Forms.Label lblComment;
    private System.Windows.Forms.TextBox txtComment;
    private System.Windows.Forms.Button btnOk;
    private System.Windows.Forms.Button btnCancel;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        this.lblIssueId = new System.Windows.Forms.Label();
        this.txtIssueId = new System.Windows.Forms.TextBox();
        this.lblComment = new System.Windows.Forms.Label();
        this.txtComment = new System.Windows.Forms.TextBox();
        this.btnOk = new System.Windows.Forms.Button();
        this.btnCancel = new System.Windows.Forms.Button();
        this.SuspendLayout();

        this.lblIssueId.AutoSize = true;
        this.lblIssueId.Location = new System.Drawing.Point(12, 15);
        this.lblIssueId.Name = "lblIssueId";
        this.lblIssueId.Size = new System.Drawing.Size(63, 15);
        this.lblIssueId.Text = "チケット番号";

        this.txtIssueId.Location = new System.Drawing.Point(94, 12);
        this.txtIssueId.Name = "txtIssueId";
        this.txtIssueId.Size = new System.Drawing.Size(278, 23);

        this.lblComment.AutoSize = true;
        this.lblComment.Location = new System.Drawing.Point(12, 50);
        this.lblComment.Name = "lblComment";
        this.lblComment.Size = new System.Drawing.Size(45, 15);
        this.lblComment.Text = "コメント";

        this.txtComment.Location = new System.Drawing.Point(94, 47);
        this.txtComment.Multiline = true;
        this.txtComment.Name = "txtComment";
        this.txtComment.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
        this.txtComment.Size = new System.Drawing.Size(278, 132);

        this.btnOk.Location = new System.Drawing.Point(216, 192);
        this.btnOk.Name = "btnOk";
        this.btnOk.Size = new System.Drawing.Size(75, 27);
        this.btnOk.Text = "追加";
        this.btnOk.UseVisualStyleBackColor = true;
        this.btnOk.Click += new System.EventHandler(this.btnOk_Click);

        this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
        this.btnCancel.Location = new System.Drawing.Point(297, 192);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new System.Drawing.Size(75, 27);
        this.btnCancel.Text = "キャンセル";
        this.btnCancel.UseVisualStyleBackColor = true;

        this.AcceptButton = this.btnOk;
        this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.CancelButton = this.btnCancel;
        this.ClientSize = new System.Drawing.Size(384, 231);
        this.Controls.Add(this.lblIssueId);
        this.Controls.Add(this.txtIssueId);
        this.Controls.Add(this.lblComment);
        this.Controls.Add(this.txtComment);
        this.Controls.Add(this.btnOk);
        this.Controls.Add(this.btnCancel);
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
        this.MaximizeBox = false;
        this.MinimizeBox = false;
        this.Name = "AddCommentDialog";
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
        this.Text = "コメント追記";
        this.ResumeLayout(false);
        this.PerformLayout();
    }
}