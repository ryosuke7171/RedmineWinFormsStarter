using System.Data;
using System.Text;

namespace RedmineWinFormsStarter;

internal sealed class CsvWorkspaceSession
{
    // LINKTAG RMCSV002
    private CsvWorkspaceSession(DataTable table, string workingFilePath, string sourceDescription, string detectedEncoding)
    {
        Table = table;
        WorkingFilePath = workingFilePath;
        SourceDescription = sourceDescription;
        DetectedEncoding = detectedEncoding;
    }

    public DataTable Table { get; }

    public string WorkingFilePath { get; }

    public string SourceDescription { get; }

    public string DetectedEncoding { get; }

    public void Save()
    {
        CsvLoader.Save(WorkingFilePath, Table);
    }

    public static CsvWorkspaceSession ImportLocalFile(string sourcePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourcePath);

        var bytes = File.ReadAllBytes(sourcePath);
        return Create(bytes, $"ローカルCSV: {sourcePath}", Path.GetFileNameWithoutExtension(sourcePath));
    }

    public static CsvWorkspaceSession ImportDownloadedBytes(byte[] content, string sourceDescription, string fileNameStem)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDescription);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileNameStem);

        return Create(content, sourceDescription, fileNameStem);
    }

    private static CsvWorkspaceSession Create(byte[] content, string sourceDescription, string fileNameStem)
    {
        ArgumentNullException.ThrowIfNull(content);
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceDescription);
        ArgumentException.ThrowIfNullOrWhiteSpace(fileNameStem);

        var table = CsvLoader.Load(content);
        var detectedEncoding = table.ExtendedProperties["DetectedEncoding"] as string ?? CsvLoader.GetEncodingDisplayName(Encoding.UTF8);
        var workingFilePath = CreateWorkingFilePath(fileNameStem);
        CsvLoader.Save(workingFilePath, table);
        return new CsvWorkspaceSession(table, workingFilePath, sourceDescription, detectedEncoding);
    }

    private static string CreateWorkingFilePath(string fileNameStem)
    {
        var safeName = SanitizeFileName(fileNameStem);
        var directory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RedmineWinFormsStarter",
            "WorkingCsv");

        Directory.CreateDirectory(directory);

        var fileName = $"{DateTime.Now:yyyyMMdd_HHmmss}_{safeName}.csv";
        return Path.Combine(directory, fileName);
    }

    private static string SanitizeFileName(string value)
    {
        var invalidChars = Path.GetInvalidFileNameChars();
        var buffer = value
            .Where(ch => !invalidChars.Contains(ch))
            .Select(ch => char.IsWhiteSpace(ch) ? '_' : ch)
            .ToArray();

        var sanitized = new string(buffer).Trim('_');
        return string.IsNullOrWhiteSpace(sanitized) ? "issues" : sanitized;
    }
}