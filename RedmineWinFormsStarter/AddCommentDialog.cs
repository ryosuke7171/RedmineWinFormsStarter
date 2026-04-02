using System.Windows.Forms;

namespace RedmineWinFormsStarter;

internal partial class AddCommentDialog : Form
{
    public AddCommentDialog()
    {
        InitializeComponent();
    }

    public int IssueId
    {
        get
        {
            if (!int.TryParse(txtIssueId.Text.Trim(), out int issueId) || issueId <= 0)
            {
                throw new InvalidOperationException("チケット番号は正の整数で入力してください。");
            }

            return issueId;
        }
    }

    public string CommentText
    {
        get
        {
            string commentText = txtComment.Text.Trim();
            if (string.IsNullOrWhiteSpace(commentText))
            {
                throw new InvalidOperationException("コメントを入力してください。");
            }

            return commentText;
        }
    }

    private void btnOk_Click(object? sender, EventArgs e)
    {
        try
        {
            _ = IssueId;
            _ = CommentText;
            DialogResult = DialogResult.OK;
            Close();
        }
        catch (InvalidOperationException ex)
        {
            MessageBox.Show(this, ex.Message, "コメント追記", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }
}