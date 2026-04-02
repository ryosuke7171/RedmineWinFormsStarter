using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace RedmineWinFormsStarter;

internal partial class CsvLoadDialog : Form
{
    private readonly IReadOnlyList<RedmineProjectChoice> _projects;

    public CsvLoadDialog(IReadOnlyList<RedmineProjectChoice> projects, bool canUseRemote)
    {
        ArgumentNullException.ThrowIfNull(projects);

        _projects = projects;
        InitializeComponent();

        cmbProjects.DisplayMember = nameof(RedmineProjectChoice.Name);
        cmbProjects.ValueMember = nameof(RedmineProjectChoice.Identifier);
        cmbProjects.DataSource = projects.ToList();

        rdoUrlDownload.Enabled = canUseRemote;
        rdoProjectDownload.Enabled = canUseRemote && projects.Count > 0;

        if (!canUseRemote)
        {
            rdoLocalFile.Checked = true;
        }
        else if (projects.Count > 0)
        {
            rdoProjectDownload.Checked = true;
        }

        UpdateModeState();
    }

    public CsvLoadMode SelectedMode => rdoUrlDownload.Checked
        ? CsvLoadMode.DownloadFromUrl
        : rdoProjectDownload.Checked
            ? CsvLoadMode.DownloadFromProject
            : CsvLoadMode.LocalFile;

    public string LocalPath => txtLocalPath.Text.Trim();

    public string DownloadUrl => txtDownloadUrl.Text.Trim();

    public RedmineProjectChoice? SelectedProject => cmbProjects.SelectedItem as RedmineProjectChoice;

    private void btnBrowseLocal_Click(object? sender, EventArgs e)
    {
        using var dialog = new OpenFileDialog
        {
            Filter = "CSV (*.csv)|*.csv",
            Title = "Select issues.csv"
        };

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            txtLocalPath.Text = dialog.FileName;
        }
    }

    private void ModeChanged(object? sender, EventArgs e)
    {
        UpdateModeState();
    }

    private void btnOk_Click(object? sender, EventArgs e)
    {
        if (!ValidateSelection())
        {
            DialogResult = DialogResult.None;
            return;
        }

        DialogResult = DialogResult.OK;
        Close();
    }

    private void UpdateModeState()
    {
        txtLocalPath.Enabled = rdoLocalFile.Checked;
        btnBrowseLocal.Enabled = rdoLocalFile.Checked;
        txtDownloadUrl.Enabled = rdoUrlDownload.Checked;
        cmbProjects.Enabled = rdoProjectDownload.Checked;
    }

    private bool ValidateSelection()
    {
        if (rdoLocalFile.Checked)
        {
            if (string.IsNullOrWhiteSpace(LocalPath))
            {
                MessageBox.Show("ローカルCSVファイルを選択してください。", "Load CSV", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        if (rdoUrlDownload.Checked)
        {
            if (string.IsNullOrWhiteSpace(DownloadUrl))
            {
                MessageBox.Show("CSVダウンロードURLを入力してください。", "Load CSV", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        if (rdoProjectDownload.Checked && SelectedProject is null)
        {
            MessageBox.Show("プロジェクトを選択してください。", "Load CSV", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return false;
        }

        return true;
    }
}

internal enum CsvLoadMode
{
    LocalFile,
    DownloadFromUrl,
    DownloadFromProject
}