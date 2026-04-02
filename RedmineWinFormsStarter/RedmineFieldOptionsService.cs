using System.Collections.Generic;
using System.Data;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace RedmineWinFormsStarter;

internal sealed class RedmineFieldOptionsService : IDisposable
{
    // LINKTAG RMCSV003
    // LINKTAG RMMETA001
    private static readonly string[] ExportBaseColumns =
    [
        "#",
        "プロジェクト",
        "トラッカー",
        "親チケット",
        "ステータス",
        "優先度",
        "題名",
        "作成者",
        "担当者",
        "更新日",
        "カテゴリ",
        "対象バージョン",
        "開始日",
        "期日",
        "進捗率",
        "作成日",
        "終了日",
        "プライベート",
        "説明",
        "最新のコメント"
    ];

    private static readonly HashSet<string> StandardIssueColumns =
    [
        "#",
        "プロジェクト",
        "トラッカー",
        "親チケット",
        "親チケットの題名",
        "ステータス",
        "優先度",
        "題名",
        "作成者",
        "担当者",
        "更新日",
        "カテゴリ",
        "対象バージョン",
        "開始日",
        "期日",
        "予定工数",
        "合計予定工数",
        "作業時間",
        "合計作業時間",
        "進捗率",
        "作成日",
        "終了日",
        "最終更新者",
        "関連するチケット",
        "ファイル",
        "成果物ファイル名",
        "プライベート",
        "説明",
        "最新のコメント",
        "変更理由・内容"
    ];

    private readonly HttpClient _client;
    private readonly Dictionary<string, ProjectInfo> _projectsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ProjectInfo> _projectsByIdentifier = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, CustomFieldInfo> _customFieldsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _statusIdsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _priorityIdsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, int> _trackerIdsByName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _defaultStatusesByTrackerName = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<string>> _trackersByProjectIdentifier = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<string>> _assigneesByProjectIdentifier = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, int>> _assigneeIdsByProjectIdentifier = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<string>> _versionsByProjectIdentifier = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, int>> _versionIdsByProjectIdentifier = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, IReadOnlyList<string>> _categoriesByProjectIdentifier = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, Dictionary<string, int>> _categoryIdsByProjectIdentifier = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, IssueInfo> _issuesById = [];
    private IReadOnlyList<string> _projectNames = [];
    private IReadOnlyList<string> _trackerNames = [];
    private IReadOnlyList<string> _priorityNames = [];
    private IReadOnlyList<string> _statusNames = [];
    private bool _initialized;

    public RedmineFieldOptionsService(string baseUrl, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new ArgumentException("RedmineURL は必須です", nameof(baseUrl));
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API Key は必須です", nameof(apiKey));
        }

        var normalizedBaseUrl = baseUrl.Trim().TrimEnd('/') + "/";
        _client = new HttpClient
        {
            BaseAddress = new Uri(normalizedBaseUrl, UriKind.Absolute),
            Timeout = TimeSpan.FromSeconds(30)
        };
        _client.DefaultRequestHeaders.Add("X-Redmine-API-Key", apiKey.Trim());
        _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        _statusIdsByName.Clear();
        _priorityIdsByName.Clear();
        _trackerIdsByName.Clear();
        _defaultStatusesByTrackerName.Clear();
        _trackersByProjectIdentifier.Clear();

        _statusNames = await LoadNameIdListAsync("issue_statuses.json", "issue_statuses", _statusIdsByName, cancellationToken).ConfigureAwait(false);
        _priorityNames = await LoadNameIdListAsync("enumerations/issue_priorities.json", "issue_priorities", _priorityIdsByName, cancellationToken).ConfigureAwait(false);
        _trackerNames = await LoadTrackersAsync(cancellationToken).ConfigureAwait(false);
        await LoadProjectsAsync(cancellationToken).ConfigureAwait(false);
        await LoadCustomFieldsAsync(cancellationToken).ConfigureAwait(false);
        _initialized = true;
    }

    public IReadOnlyList<RedmineProjectChoice> GetProjectChoices()
        => _projectsByName.Values
            .OrderBy(project => project.Name, StringComparer.OrdinalIgnoreCase)
            .Select(project => new RedmineProjectChoice(project.Name, project.Identifier))
            .ToArray();

    public async Task<byte[]> DownloadIssuesCsvAsync(string csvUrlOrPath, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(csvUrlOrPath);

        var requestUri = BuildCsvUri(csvUrlOrPath);
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.Clear();
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/csv"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

        using var response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        var payload = await response.Content.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            var error = Encoding.UTF8.GetString(payload);
            throw new HttpRequestException($"Redmine CSV download error ({(int)response.StatusCode}): {error}");
        }

        return payload;
    }

    public async Task<byte[]> DownloadIssuesCsvForProjectAsync(string projectIdentifier, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(projectIdentifier);
        EnsureInitialized();

        var issues = await GetProjectIssuesAsync(projectIdentifier, cancellationToken).ConfigureAwait(false);
        var customFieldNames = CollectCustomFieldNames(issues);
        var table = CreateProjectExportTable(customFieldNames);

        foreach (var issue in issues)
        {
            var row = table.NewRow();
            var customFields = ReadCustomFieldValues(issue);

            row["#"] = TryGetInt(issue, "id")?.ToString() ?? string.Empty;
            row["プロジェクト"] = TryGetNestedName(issue, "project");
            row["トラッカー"] = TryGetNestedName(issue, "tracker");
            row["親チケット"] = TryGetNestedId(issue, "parent")?.ToString() ?? string.Empty;
            row["ステータス"] = TryGetNestedName(issue, "status");
            row["優先度"] = TryGetNestedName(issue, "priority");
            row["題名"] = TryGetString(issue, "subject");
            row["作成者"] = TryGetNestedName(issue, "author");
            row["担当者"] = TryGetNestedName(issue, "assigned_to");
            row["更新日"] = TryGetString(issue, "updated_on");
            row["カテゴリ"] = TryGetNestedName(issue, "category");
            row["対象バージョン"] = TryGetNestedName(issue, "fixed_version");
            row["開始日"] = TryGetString(issue, "start_date");
            row["期日"] = TryGetString(issue, "due_date");
            row["進捗率"] = TryGetInt(issue, "done_ratio")?.ToString() ?? string.Empty;
            row["作成日"] = TryGetString(issue, "created_on");
            row["終了日"] = TryGetString(issue, "closed_on");
            row["プライベート"] = TryGetBoolean(issue, "is_private") switch
            {
                true => "はい",
                false => "いいえ",
                _ => string.Empty
            };
            row["説明"] = TryGetString(issue, "description");
            row["最新のコメント"] = string.Empty;

            foreach (var customFieldName in customFieldNames)
            {
                row[customFieldName] = customFields.TryGetValue(customFieldName, out var value) ? value : string.Empty;
            }

            table.Rows.Add(row);
        }

        return CsvLoader.SaveToBytes(table);
    }

    public async Task<IReadOnlyList<CreatedIssueInfo>> CreateIssuesAsync(IEnumerable<IReadOnlyDictionary<string, string>> issueRows, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(issueRows);
        EnsureInitialized();

        var createdIssues = new List<CreatedIssueInfo>();
        foreach (var issueRow in issueRows)
        {
            var normalizedRow = NormalizeRow(issueRow);
            var payload = await BuildIssuePayloadAsync(normalizedRow, cancellationToken).ConfigureAwait(false);
            using var content = JsonContent.Create(payload);
            using var response = await _client.PostAsync("issues.json", content, cancellationToken).ConfigureAwait(false);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Redmine API error ({(int)response.StatusCode}): {responseBody}");
            }

            using var document = JsonDocument.Parse(responseBody);
            if (!document.RootElement.TryGetProperty("issue", out var issueElement))
            {
                throw new InvalidOperationException("作成したチケット情報を Redmine から取得できませんでした。");
            }

            normalizedRow["#"] = TryGetInt(issueElement, "id")?.ToString() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(GetValue(normalizedRow, "ステータス")))
            {
                normalizedRow["ステータス"] = TryGetNestedName(issueElement, "status");
            }

            createdIssues.Add(new CreatedIssueInfo(Convert.ToInt32(normalizedRow["#"]), normalizedRow));
        }

        return createdIssues;
    }

    public async Task<IReadOnlyList<RedmineIssueComment>> GetIssueCommentsAsync(IEnumerable<int> issueIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(issueIds);

        var comments = new List<RedmineIssueComment>();
        foreach (var issueId in issueIds.Distinct().Order())
        {
            using var document = await GetJsonAsync($"issues/{issueId}.json?include=journals", cancellationToken).ConfigureAwait(false);
            if (!document.RootElement.TryGetProperty("issue", out var issueElement)
                || !issueElement.TryGetProperty("journals", out var journalsElement)
                || journalsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var journalElement in journalsElement.EnumerateArray())
            {
                var notes = TryGetString(journalElement, "notes");
                if (string.IsNullOrWhiteSpace(notes))
                {
                    continue;
                }

                var journalId = TryGetInt(journalElement, "id");
                if (!journalId.HasValue)
                {
                    continue;
                }

                comments.Add(new RedmineIssueComment(
                    issueId,
                    journalId.Value,
                    TryGetNestedName(journalElement, "user"),
                    TryGetString(journalElement, "created_on"),
                    notes));
            }
        }

        return comments;
    }

    public async Task AddIssueCommentAsync(int issueId, string commentText, CancellationToken cancellationToken)
    {
        if (issueId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(issueId));
        }

        if (string.IsNullOrWhiteSpace(commentText))
        {
            throw new ArgumentException("コメントは必須です。", nameof(commentText));
        }

        var payload = new
        {
            issue = new
            {
                notes = commentText.Trim()
            }
        };

        await PutJsonAsync($"issues/{issueId}.json", payload, cancellationToken).ConfigureAwait(false);
    }

    public async Task<RedmineCellConstraint?> GetConstraintAsync(DataRow row, string columnName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(row);
        ArgumentException.ThrowIfNullOrWhiteSpace(columnName);
        EnsureInitialized();

        return columnName switch
        {
            "プロジェクト" => CreateConstraint(_projectNames),
            "トラッカー" => CreateConstraint(await GetTrackerOptionsAsync(row, cancellationToken).ConfigureAwait(false)),
            "優先度" => CreateConstraint(_priorityNames),
            "ステータス" => CreateConstraint(await GetStatusOptionsAsync(row, cancellationToken).ConfigureAwait(false)),
            "担当者" => CreateConstraint(await GetProjectMembersAsync(row, cancellationToken).ConfigureAwait(false)),
            "対象バージョン" => CreateConstraint(await GetProjectVersionsAsync(row, cancellationToken).ConfigureAwait(false)),
            "カテゴリ" => CreateConstraint(await GetProjectCategoriesAsync(row, cancellationToken).ConfigureAwait(false)),
            _ => await GetCustomFieldConstraintAsync(row, columnName, cancellationToken).ConfigureAwait(false)
        };
    }

    public static IReadOnlyList<string> SplitSelections(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        var normalized = value
            .Replace('、', ',')
            .Replace('，', ',')
            .Replace(';', ',')
            .Replace('；', ',');

        return normalized
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(selection => !string.IsNullOrWhiteSpace(selection))
            .ToArray();
    }

    private async Task<object> BuildIssuePayloadAsync(IReadOnlyDictionary<string, string> row, CancellationToken cancellationToken)
    {
        var projectName = GetValue(row, "プロジェクト");
        if (string.IsNullOrWhiteSpace(projectName) || !_projectsByName.TryGetValue(projectName, out var project))
        {
            throw new InvalidOperationException($"プロジェクト名が見つかりません: {projectName}");
        }

        var trackerName = GetValue(row, "トラッカー");
        if (string.IsNullOrWhiteSpace(trackerName) || !_trackerIdsByName.TryGetValue(trackerName, out var trackerId))
        {
            throw new InvalidOperationException($"トラッカー名が見つかりません: {trackerName}");
        }

        var subject = GetValue(row, "題名");
        if (string.IsNullOrWhiteSpace(subject))
        {
            throw new InvalidOperationException("題名は必須です。");
        }

        var issue = new Dictionary<string, object?>
        {
            ["project_id"] = project.Id,
            ["tracker_id"] = trackerId,
            ["subject"] = subject
        };

        var description = GetValue(row, "説明");
        if (!string.IsNullOrWhiteSpace(description))
        {
            issue["description"] = description;
        }

        var statusName = GetValue(row, "ステータス");
        if (!string.IsNullOrWhiteSpace(statusName))
        {
            if (!_statusIdsByName.TryGetValue(statusName, out var statusId))
            {
                throw new InvalidOperationException($"ステータス名が見つかりません: {statusName}");
            }

            issue["status_id"] = statusId;
        }

        var priorityName = GetValue(row, "優先度");
        if (!string.IsNullOrWhiteSpace(priorityName))
        {
            if (!_priorityIdsByName.TryGetValue(priorityName, out var priorityId))
            {
                throw new InvalidOperationException($"優先度名が見つかりません: {priorityName}");
            }

            issue["priority_id"] = priorityId;
        }

        var assigneeName = GetValue(row, "担当者");
        if (!string.IsNullOrWhiteSpace(assigneeName))
        {
            await EnsureProjectMembersAsync(project.Identifier, cancellationToken).ConfigureAwait(false);
            if (!_assigneeIdsByProjectIdentifier.TryGetValue(project.Identifier, out var memberIds)
                || !memberIds.TryGetValue(assigneeName, out var assigneeId))
            {
                throw new InvalidOperationException($"担当者名がプロジェクトメンバーに存在しません: {assigneeName}");
            }

            issue["assigned_to_id"] = assigneeId;
        }

        var startDate = NormalizeDate(GetValue(row, "開始日"));
        if (!string.IsNullOrWhiteSpace(startDate))
        {
            issue["start_date"] = startDate;
        }

        var dueDate = NormalizeDate(GetValue(row, "期日"));
        if (!string.IsNullOrWhiteSpace(dueDate))
        {
            issue["due_date"] = dueDate;
        }

        var doneRatio = ParseInteger(GetValue(row, "進捗率"));
        if (doneRatio.HasValue)
        {
            issue["done_ratio"] = doneRatio.Value;
        }

        var parentIssueId = ParseInteger(GetValue(row, "親チケット"));
        if (parentIssueId.HasValue)
        {
            issue["parent_issue_id"] = parentIssueId.Value;
        }

        var isPrivate = ParseJapaneseBoolean(GetValue(row, "プライベート"));
        if (isPrivate.HasValue)
        {
            issue["is_private"] = isPrivate.Value;
        }

        var versionName = GetValue(row, "対象バージョン");
        if (!string.IsNullOrWhiteSpace(versionName))
        {
            await EnsureProjectVersionsAsync(project.Identifier, cancellationToken).ConfigureAwait(false);
            if (!_versionIdsByProjectIdentifier.TryGetValue(project.Identifier, out var versionIds)
                || !versionIds.TryGetValue(versionName, out var versionId))
            {
                throw new InvalidOperationException($"対象バージョン名が見つかりません: {versionName}");
            }

            issue["fixed_version_id"] = versionId;
        }

        var categoryName = GetValue(row, "カテゴリ");
        if (!string.IsNullOrWhiteSpace(categoryName))
        {
            await EnsureProjectCategoriesAsync(project.Identifier, cancellationToken).ConfigureAwait(false);
            if (!_categoryIdsByProjectIdentifier.TryGetValue(project.Identifier, out var categoryIds)
                || !categoryIds.TryGetValue(categoryName, out var categoryId))
            {
                throw new InvalidOperationException($"カテゴリ名が見つかりません: {categoryName}");
            }

            issue["category_id"] = categoryId;
        }

        var customFieldValues = await BuildCustomFieldValuesAsync(row, project.Identifier, cancellationToken).ConfigureAwait(false);
        if (customFieldValues.Count > 0)
        {
            issue["custom_fields"] = customFieldValues;
        }

        return new { issue };
    }

    private async Task<List<Dictionary<string, object>>> BuildCustomFieldValuesAsync(IReadOnlyDictionary<string, string> row, string projectIdentifier, CancellationToken cancellationToken)
    {
        var values = new List<Dictionary<string, object>>();

        foreach (var entry in row)
        {
            if (StandardIssueColumns.Contains(entry.Key) || string.IsNullOrWhiteSpace(entry.Value))
            {
                continue;
            }

            if (!_customFieldsByName.TryGetValue(entry.Key, out var customField))
            {
                continue;
            }

            object? fieldValue = null;
            if (IsUserField(customField))
            {
                await EnsureProjectMembersAsync(projectIdentifier, cancellationToken).ConfigureAwait(false);
                if (!_assigneeIdsByProjectIdentifier.TryGetValue(projectIdentifier, out var memberIds))
                {
                    continue;
                }

                if (customField.AllowsMultiple)
                {
                    var ids = SplitSelections(entry.Value)
                        .Select(name => memberIds.TryGetValue(name, out var memberId) ? memberId : (int?)null)
                        .ToArray();

                    if (ids.Any(id => !id.HasValue))
                    {
                        throw new InvalidOperationException($"カスタムフィールド '{entry.Key}' に不正なユーザーが含まれています。");
                    }

                    fieldValue = ids.Select(id => id!.Value).ToArray();
                }
                else
                {
                    if (!memberIds.TryGetValue(entry.Value, out var memberId))
                    {
                        throw new InvalidOperationException($"カスタムフィールド '{entry.Key}' に不正なユーザーが含まれています。");
                    }

                    fieldValue = memberId;
                }
            }
            else if (customField.AllowsMultiple)
            {
                fieldValue = SplitSelections(entry.Value).ToArray();
            }
            else if (string.Equals(customField.FieldFormat, "bool", StringComparison.OrdinalIgnoreCase))
            {
                fieldValue = ParseJapaneseBoolean(entry.Value) switch
                {
                    true => "1",
                    false => "0",
                    _ => entry.Value
                };
            }
            else
            {
                fieldValue = entry.Value;
            }

            if (fieldValue is not null)
            {
                values.Add(new Dictionary<string, object>
                {
                    ["id"] = customField.Id,
                    ["value"] = fieldValue
                });
            }
        }

        return values;
    }

    private async Task<RedmineCellConstraint?> GetCustomFieldConstraintAsync(DataRow row, string columnName, CancellationToken cancellationToken)
    {
        if (!_customFieldsByName.TryGetValue(columnName, out var customField))
        {
            return null;
        }

        if (IsUserField(customField))
        {
            return CreateConstraint(await GetProjectMembersAsync(row, cancellationToken).ConfigureAwait(false), customField.AllowsMultiple);
        }

        if (string.Equals(customField.FieldFormat, "version", StringComparison.OrdinalIgnoreCase))
        {
            return CreateConstraint(await GetProjectVersionsAsync(row, cancellationToken).ConfigureAwait(false), customField.AllowsMultiple);
        }

        if (string.Equals(customField.FieldFormat, "bool", StringComparison.OrdinalIgnoreCase))
        {
            return CreateConstraint(["はい", "いいえ"], customField.AllowsMultiple);
        }

        if (customField.PossibleValues.Count > 0)
        {
            return CreateConstraint(customField.PossibleValues, customField.AllowsMultiple);
        }

        return string.Equals(customField.FieldFormat, "issue_status", StringComparison.OrdinalIgnoreCase)
            ? CreateConstraint(await GetStatusOptionsAsync(row, cancellationToken).ConfigureAwait(false), customField.AllowsMultiple)
            : null;
    }

    private async Task<IReadOnlyList<string>> GetStatusOptionsAsync(DataRow row, CancellationToken cancellationToken)
    {
        var issueId = GetIssueId(row);
        if (!issueId.HasValue)
        {
            return await GetStatusOptionsForCreateRowAsync(row, cancellationToken).ConfigureAwait(false);
        }

        var issueInfo = await GetIssueInfoAsync(issueId.Value, cancellationToken).ConfigureAwait(false);
        return issueInfo.AllowedStatuses.Count > 0 ? issueInfo.AllowedStatuses : _statusNames;
    }

    private async Task<IReadOnlyList<string>> GetTrackerOptionsAsync(DataRow row, CancellationToken cancellationToken)
    {
        var project = await GetProjectInfoAsync(row, cancellationToken).ConfigureAwait(false);
        if (project is null)
        {
            return _trackerNames;
        }

        await EnsureProjectTrackersAsync(project.Identifier, cancellationToken).ConfigureAwait(false);
        return _trackersByProjectIdentifier.TryGetValue(project.Identifier, out var trackers) && trackers.Count > 0
            ? trackers
            : _trackerNames;
    }

    private async Task<IReadOnlyList<string>> GetStatusOptionsForCreateRowAsync(DataRow row, CancellationToken cancellationToken)
    {
        var project = await GetProjectInfoAsync(row, cancellationToken).ConfigureAwait(false);
        if (project is null)
        {
            return _statusNames;
        }

        await EnsureProjectTrackersAsync(project.Identifier, cancellationToken).ConfigureAwait(false);

        var trackerName = GetDataRowValue(row, "トラッカー");
        if (!string.IsNullOrWhiteSpace(trackerName)
            && _defaultStatusesByTrackerName.TryGetValue(trackerName, out var trackerDefaultStatus)
            && !string.IsNullOrWhiteSpace(trackerDefaultStatus))
        {
            return [trackerDefaultStatus];
        }

        if (!_trackersByProjectIdentifier.TryGetValue(project.Identifier, out var projectTrackers) || projectTrackers.Count == 0)
        {
            return _statusNames;
        }

        var statuses = projectTrackers
            .Select(name => _defaultStatusesByTrackerName.TryGetValue(name, out var status) ? status : string.Empty)
            .Where(status => !string.IsNullOrWhiteSpace(status))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return statuses.Length > 0 ? statuses : _statusNames;
    }

    private async Task<IReadOnlyList<string>> GetProjectMembersAsync(DataRow row, CancellationToken cancellationToken)
    {
        var project = await GetProjectInfoAsync(row, cancellationToken).ConfigureAwait(false);
        if (project is null)
        {
            return [];
        }

        await EnsureProjectMembersAsync(project.Identifier, cancellationToken).ConfigureAwait(false);
        return _assigneesByProjectIdentifier.TryGetValue(project.Identifier, out var members) ? members : [];
    }

    private async Task<IReadOnlyList<string>> GetProjectVersionsAsync(DataRow row, CancellationToken cancellationToken)
    {
        var project = await GetProjectInfoAsync(row, cancellationToken).ConfigureAwait(false);
        if (project is null)
        {
            return [];
        }

        await EnsureProjectVersionsAsync(project.Identifier, cancellationToken).ConfigureAwait(false);
        return _versionsByProjectIdentifier.TryGetValue(project.Identifier, out var versions) ? versions : [];
    }

    private async Task<IReadOnlyList<string>> GetProjectCategoriesAsync(DataRow row, CancellationToken cancellationToken)
    {
        var project = await GetProjectInfoAsync(row, cancellationToken).ConfigureAwait(false);
        if (project is null)
        {
            return [];
        }

        await EnsureProjectCategoriesAsync(project.Identifier, cancellationToken).ConfigureAwait(false);
        return _categoriesByProjectIdentifier.TryGetValue(project.Identifier, out var categories) ? categories : [];
    }

    private async Task<ProjectInfo?> GetProjectInfoAsync(DataRow row, CancellationToken cancellationToken)
    {
        var projectName = GetDataRowValue(row, "プロジェクト");
        if (string.IsNullOrWhiteSpace(projectName))
        {
            var issueId = GetIssueId(row);
            if (issueId.HasValue)
            {
                projectName = (await GetIssueInfoAsync(issueId.Value, cancellationToken).ConfigureAwait(false)).ProjectName;
            }
        }

        return string.IsNullOrWhiteSpace(projectName) || !_projectsByName.TryGetValue(projectName, out var project)
            ? null
            : project;
    }

    private async Task<IssueInfo> GetIssueInfoAsync(int issueId, CancellationToken cancellationToken)
    {
        if (_issuesById.TryGetValue(issueId, out var issueInfo))
        {
            return issueInfo;
        }

        using var document = await GetJsonAsync($"issues/{issueId}.json?include=allowed_statuses", cancellationToken).ConfigureAwait(false);
        if (!document.RootElement.TryGetProperty("issue", out var issueElement))
        {
            issueInfo = new IssueInfo(string.Empty, []);
            _issuesById[issueId] = issueInfo;
            return issueInfo;
        }

        var projectName = TryGetNestedName(issueElement, "project");
        var allowedStatuses = issueElement.TryGetProperty("allowed_statuses", out var allowedStatusesElement)
            ? ParseNameValues(allowedStatusesElement)
            : [];

        issueInfo = new IssueInfo(projectName, allowedStatuses);
        _issuesById[issueId] = issueInfo;
        return issueInfo;
    }

    private async Task LoadProjectsAsync(CancellationToken cancellationToken)
    {
        _projectsByName.Clear();
        _projectsByIdentifier.Clear();

        var projectNames = new List<string>();
        var offset = 0;
        const int limit = 100;

        while (true)
        {
            using var document = await GetJsonAsync($"projects.json?limit={limit}&offset={offset}", cancellationToken).ConfigureAwait(false);
            if (!document.RootElement.TryGetProperty("projects", out var projectsElement) || projectsElement.ValueKind != JsonValueKind.Array)
            {
                break;
            }

            var count = 0;
            foreach (var projectElement in projectsElement.EnumerateArray())
            {
                count++;
                var id = TryGetInt(projectElement, "id");
                var name = TryGetString(projectElement, "name");
                var identifier = TryGetString(projectElement, "identifier");
                if (!id.HasValue || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(identifier))
                {
                    continue;
                }

                var project = new ProjectInfo(id.Value, name, identifier);
                _projectsByName[name] = project;
                _projectsByIdentifier[identifier] = project;
                projectNames.Add(name);
            }

            if (count < limit)
            {
                break;
            }

            offset += limit;
        }

        _projectNames = Distinct(projectNames);
    }

    private async Task<IReadOnlyList<string>> LoadTrackersAsync(CancellationToken cancellationToken)
    {
        using var document = await GetJsonAsync("trackers.json", cancellationToken).ConfigureAwait(false);
        if (!document.RootElement.TryGetProperty("trackers", out var trackersElement) || trackersElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var trackerNames = new List<string>();
        foreach (var trackerElement in trackersElement.EnumerateArray())
        {
            AddIdName(trackerNames, _trackerIdsByName, trackerElement);

            var trackerName = TryGetString(trackerElement, "name");
            var defaultStatusName = TryGetNestedName(trackerElement, "default_status");
            if (!string.IsNullOrWhiteSpace(trackerName) && !string.IsNullOrWhiteSpace(defaultStatusName))
            {
                _defaultStatusesByTrackerName[trackerName] = defaultStatusName;
            }
        }

        return Distinct(trackerNames);
    }

    private async Task LoadCustomFieldsAsync(CancellationToken cancellationToken)
    {
        _customFieldsByName.Clear();

        try
        {
            using var document = await GetJsonAsync("custom_fields.json", cancellationToken).ConfigureAwait(false);
            if (!document.RootElement.TryGetProperty("custom_fields", out var customFieldsElement) || customFieldsElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var customFieldElement in customFieldsElement.EnumerateArray())
            {
                var id = TryGetInt(customFieldElement, "id");
                var name = TryGetString(customFieldElement, "name");
                if (!id.HasValue || string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var fieldFormat = TryGetString(customFieldElement, "field_format");
                var allowsMultiple = customFieldElement.TryGetProperty("multiple", out var multipleElement) && multipleElement.ValueKind == JsonValueKind.True;
                var possibleValues = customFieldElement.TryGetProperty("possible_values", out var possibleValuesElement)
                    ? ParsePossibleValues(possibleValuesElement)
                    : [];

                _customFieldsByName[name] = new CustomFieldInfo(id.Value, name, fieldFormat, allowsMultiple, possibleValues);
            }
        }
        catch (HttpRequestException)
        {
        }
    }

    private async Task EnsureProjectMembersAsync(string projectIdentifier, CancellationToken cancellationToken)
    {
        if (_assigneesByProjectIdentifier.ContainsKey(projectIdentifier))
        {
            return;
        }

        var members = new List<string>();
        var memberIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var offset = 0;
        const int limit = 100;

        while (true)
        {
            using var document = await GetJsonAsync($"projects/{Uri.EscapeDataString(projectIdentifier)}/memberships.json?limit={limit}&offset={offset}", cancellationToken).ConfigureAwait(false);
            if (!document.RootElement.TryGetProperty("memberships", out var membershipsElement) || membershipsElement.ValueKind != JsonValueKind.Array)
            {
                break;
            }

            var count = 0;
            foreach (var membership in membershipsElement.EnumerateArray())
            {
                count++;
                AddNamedEntity(members, memberIds, membership, "user");
                AddNamedEntity(members, memberIds, membership, "group");
            }

            if (count < limit)
            {
                break;
            }

            offset += limit;
        }

        _assigneesByProjectIdentifier[projectIdentifier] = Distinct(members);
        _assigneeIdsByProjectIdentifier[projectIdentifier] = memberIds;
    }

    private async Task EnsureProjectVersionsAsync(string projectIdentifier, CancellationToken cancellationToken)
    {
        if (_versionsByProjectIdentifier.ContainsKey(projectIdentifier))
        {
            return;
        }

        var versionNames = new List<string>();
        var versionIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var offset = 0;
        const int limit = 100;

        while (true)
        {
            using var document = await GetJsonAsync($"projects/{Uri.EscapeDataString(projectIdentifier)}/versions.json?limit={limit}&offset={offset}", cancellationToken).ConfigureAwait(false);
            if (!document.RootElement.TryGetProperty("versions", out var versionsElement) || versionsElement.ValueKind != JsonValueKind.Array)
            {
                break;
            }

            var count = 0;
            foreach (var version in versionsElement.EnumerateArray())
            {
                count++;
                AddIdName(versionNames, versionIds, version);
            }

            if (count < limit)
            {
                break;
            }

            offset += limit;
        }

        _versionsByProjectIdentifier[projectIdentifier] = Distinct(versionNames);
        _versionIdsByProjectIdentifier[projectIdentifier] = versionIds;
    }

    private async Task EnsureProjectCategoriesAsync(string projectIdentifier, CancellationToken cancellationToken)
    {
        if (_categoriesByProjectIdentifier.ContainsKey(projectIdentifier))
        {
            return;
        }

        var categoryNames = new List<string>();
        var categoryIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        using var document = await GetJsonAsync($"projects/{Uri.EscapeDataString(projectIdentifier)}/issue_categories.json", cancellationToken).ConfigureAwait(false);
        if (document.RootElement.TryGetProperty("issue_categories", out var categoriesElement) && categoriesElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var category in categoriesElement.EnumerateArray())
            {
                AddIdName(categoryNames, categoryIds, category);
            }
        }

        _categoriesByProjectIdentifier[projectIdentifier] = Distinct(categoryNames);
        _categoryIdsByProjectIdentifier[projectIdentifier] = categoryIds;
    }

    private async Task EnsureProjectTrackersAsync(string projectIdentifier, CancellationToken cancellationToken)
    {
        if (_trackersByProjectIdentifier.ContainsKey(projectIdentifier))
        {
            return;
        }

        using var document = await GetJsonAsync($"projects/{Uri.EscapeDataString(projectIdentifier)}.json?include=trackers", cancellationToken).ConfigureAwait(false);
        if (!document.RootElement.TryGetProperty("project", out var projectElement))
        {
            _trackersByProjectIdentifier[projectIdentifier] = _trackerNames;
            return;
        }

        if (!projectElement.TryGetProperty("trackers", out var trackersElement) || trackersElement.ValueKind != JsonValueKind.Array)
        {
            _trackersByProjectIdentifier[projectIdentifier] = _trackerNames;
            return;
        }

        var trackerNames = new List<string>();
        foreach (var trackerElement in trackersElement.EnumerateArray())
        {
            var trackerName = TryGetString(trackerElement, "name");
            if (!string.IsNullOrWhiteSpace(trackerName))
            {
                trackerNames.Add(trackerName);
            }
        }

        _trackersByProjectIdentifier[projectIdentifier] = Distinct(trackerNames);
    }

    private async Task<IReadOnlyList<JsonElement>> GetProjectIssuesAsync(string projectIdentifier, CancellationToken cancellationToken)
    {
        var issues = new List<JsonElement>();
        var offset = 0;
        const int limit = 100;

        while (true)
        {
            using var document = await GetJsonAsync($"issues.json?project_id={Uri.EscapeDataString(projectIdentifier)}&status_id=*&limit={limit}&offset={offset}&sort=id:asc", cancellationToken).ConfigureAwait(false);
            if (!document.RootElement.TryGetProperty("issues", out var issuesElement) || issuesElement.ValueKind != JsonValueKind.Array)
            {
                break;
            }

            var count = 0;
            foreach (var issue in issuesElement.EnumerateArray())
            {
                count++;
                issues.Add(issue.Clone());
            }

            if (count < limit)
            {
                break;
            }

            offset += limit;
        }

        return issues;
    }

    private async Task<IReadOnlyList<string>> LoadNameIdListAsync(string path, string propertyName, Dictionary<string, int> destination, CancellationToken cancellationToken)
    {
        destination.Clear();
        using var document = await GetJsonAsync(path, cancellationToken).ConfigureAwait(false);
        if (!document.RootElement.TryGetProperty(propertyName, out var itemsElement) || itemsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var names = new List<string>();
        foreach (var item in itemsElement.EnumerateArray())
        {
            AddIdName(names, destination, item);
        }

        return Distinct(names);
    }

    private async Task<JsonDocument> GetJsonAsync(string relativePath, CancellationToken cancellationToken)
    {
        using var response = await _client.GetAsync(relativePath, cancellationToken).ConfigureAwait(false);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Redmine API error ({(int)response.StatusCode}): {payload}");
        }

        return JsonDocument.Parse(payload);
    }

    private async Task PutJsonAsync(string relativePath, object payload, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(relativePath);
        ArgumentNullException.ThrowIfNull(payload);

        using var content = JsonContent.Create(payload);
        using var response = await _client.PutAsync(relativePath, content, cancellationToken).ConfigureAwait(false);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"Redmine API error ({(int)response.StatusCode}): {responseBody}");
        }
    }

    private static RedmineCellConstraint? CreateConstraint(IReadOnlyList<string> options, bool allowsMultiple = false)
        => options.Count == 0 ? null : new RedmineCellConstraint(options, allowsMultiple);

    private static IReadOnlyList<string> ParseNameValues(JsonElement itemsElement)
    {
        if (itemsElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var values = new List<string>();
        foreach (var item in itemsElement.EnumerateArray())
        {
            var name = TryGetString(item, "name");
            if (!string.IsNullOrWhiteSpace(name))
            {
                values.Add(name);
            }
        }

        return Distinct(values);
    }

    private static IReadOnlyList<string> ParsePossibleValues(JsonElement possibleValuesElement)
    {
        if (possibleValuesElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var values = new List<string>();
        foreach (var item in possibleValuesElement.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                var rawValue = item.GetString();
                if (!string.IsNullOrWhiteSpace(rawValue))
                {
                    values.Add(rawValue);
                }

                continue;
            }

            var value = TryGetString(item, "value");
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
                continue;
            }

            value = TryGetString(item, "name");
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
                continue;
            }

            value = TryGetString(item, "label");
            if (!string.IsNullOrWhiteSpace(value))
            {
                values.Add(value);
            }
        }

        return Distinct(values);
    }

    private static IReadOnlyList<string> CollectCustomFieldNames(IEnumerable<JsonElement> issues)
    {
        ArgumentNullException.ThrowIfNull(issues);

        var names = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var issue in issues)
        {
            foreach (var fieldName in ReadCustomFieldValues(issue).Keys)
            {
                if (seen.Add(fieldName))
                {
                    names.Add(fieldName);
                }
            }
        }

        return names;
    }

    private static Dictionary<string, string> ReadCustomFieldValues(JsonElement issue)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (!issue.TryGetProperty("custom_fields", out var customFieldsElement) || customFieldsElement.ValueKind != JsonValueKind.Array)
        {
            return values;
        }

        foreach (var customField in customFieldsElement.EnumerateArray())
        {
            var name = TryGetString(customField, "name");
            if (string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var value = customField.TryGetProperty("value", out var valueElement)
                ? ConvertJsonValueToString(valueElement)
                : string.Empty;

            values[name] = value;
        }

        return values;
    }

    private static string ConvertJsonValueToString(JsonElement valueElement)
    {
        return valueElement.ValueKind switch
        {
            JsonValueKind.Array => string.Join(", ", valueElement.EnumerateArray().Select(ConvertJsonValueToString).Where(value => !string.IsNullOrWhiteSpace(value))),
            JsonValueKind.Object => TryGetString(valueElement, "name") is var name && !string.IsNullOrWhiteSpace(name)
                ? name
                : TryGetString(valueElement, "value"),
            JsonValueKind.String => valueElement.GetString() ?? string.Empty,
            JsonValueKind.Number => valueElement.ToString(),
            JsonValueKind.True => "はい",
            JsonValueKind.False => "いいえ",
            _ => string.Empty
        };
    }

    private static DataTable CreateProjectExportTable(IReadOnlyList<string> customFieldNames)
    {
        var table = new DataTable();
        foreach (var columnName in ExportBaseColumns.Concat(customFieldNames))
        {
            var column = table.Columns.Add(columnName);
            column.Caption = columnName;
        }

        table.ExtendedProperties["DisplayHeaders"] = table.Columns.Cast<DataColumn>().Select(column => column.Caption).ToList();
        table.ExtendedProperties["ColumnNames"] = table.Columns.Cast<DataColumn>().Select(column => column.ColumnName).ToList();
        table.ExtendedProperties["DetectedEncoding"] = "UTF-8";
        return table;
    }

    private static void AddNamedEntity(List<string> names, Dictionary<string, int> ids, JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var nested) || nested.ValueKind != JsonValueKind.Object)
        {
            return;
        }

        AddIdName(names, ids, nested);
    }

    private static void AddIdName(List<string> names, Dictionary<string, int> ids, JsonElement element)
    {
        var id = TryGetInt(element, "id");
        var name = TryGetString(element, "name");
        if (!id.HasValue || string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        ids[name] = id.Value;
        names.Add(name);
    }

    private static bool IsUserField(CustomFieldInfo customField)
        => string.Equals(customField.FieldFormat, "user", StringComparison.OrdinalIgnoreCase)
            || customField.Name.Contains("レビュアー", StringComparison.OrdinalIgnoreCase)
            || customField.Name.Contains("承認者", StringComparison.OrdinalIgnoreCase)
            || customField.Name.Contains("担当", StringComparison.OrdinalIgnoreCase)
            || customField.Name.Contains("責任者", StringComparison.OrdinalIgnoreCase)
            || customField.Name.Contains("副担当", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeDate(string value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().Replace('/', '-');

    private static int? ParseInteger(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return int.TryParse(value.Trim(), out var parsedValue) ? parsedValue : null;
    }

    private static bool? ParseJapaneseBoolean(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Trim() switch
        {
            "はい" or "true" or "TRUE" or "1" or "Yes" or "YES" => true,
            "いいえ" or "false" or "FALSE" or "0" or "No" or "NO" => false,
            _ => null
        };
    }

    private static string GetValue(IReadOnlyDictionary<string, string> row, string columnName)
        => row.TryGetValue(columnName, out var value) ? value.Trim() : string.Empty;

    private static Dictionary<string, string> NormalizeRow(IReadOnlyDictionary<string, string> row)
    {
        var normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in row)
        {
            normalized[entry.Key] = entry.Value?.Trim() ?? string.Empty;
        }

        return normalized;
    }

    private static string GetDataRowValue(DataRow row, string columnName)
    {
        if (!row.Table.Columns.Contains(columnName))
        {
            return string.Empty;
        }

        return Convert.ToString(row[columnName])?.Trim() ?? string.Empty;
    }

    private static int? GetIssueId(DataRow row)
    {
        var raw = GetDataRowValue(row, "#");
        if (string.IsNullOrWhiteSpace(raw))
        {
            raw = GetDataRowValue(row, "﻿#");
        }

        return int.TryParse(raw, out var issueId) ? issueId : null;
    }

    private static string TryGetNestedName(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var nestedElement) || nestedElement.ValueKind != JsonValueKind.Object)
        {
            return string.Empty;
        }

        return TryGetString(nestedElement, "name");
    }

    private static int? TryGetNestedId(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var nestedElement) || nestedElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        return TryGetInt(nestedElement, "id");
    }

    private static string TryGetString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var propertyValue))
        {
            return string.Empty;
        }

        return propertyValue.ValueKind == JsonValueKind.String
            ? propertyValue.GetString() ?? string.Empty
            : string.Empty;
    }

    private static int? TryGetInt(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var propertyValue))
        {
            return null;
        }

        return propertyValue.ValueKind == JsonValueKind.Number && propertyValue.TryGetInt32(out var value)
            ? value
            : null;
    }

    private static bool? TryGetBoolean(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var propertyValue))
        {
            return null;
        }

        return propertyValue.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null
        };
    }

    private static IReadOnlyList<string> Distinct(IEnumerable<string> values)
    {
        var list = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var value in values)
        {
            if (string.IsNullOrWhiteSpace(value) || !seen.Add(value))
            {
                continue;
            }

            list.Add(value);
        }

        return list;
    }

    private void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException("Redmine field options have not been initialized.");
        }
    }

    private Uri BuildCsvUri(string csvUrlOrPath)
    {
        if (Uri.TryCreate(csvUrlOrPath, UriKind.Absolute, out var absoluteUri))
        {
            if (_client.BaseAddress is null || Uri.Compare(_client.BaseAddress, absoluteUri, UriComponents.SchemeAndServer, UriFormat.Unescaped, StringComparison.OrdinalIgnoreCase) != 0)
            {
                throw new InvalidOperationException("CSV ダウンロード URL は Redmine と同じホストを指定してください。");
            }

            return absoluteUri;
        }

        return new Uri(_client.BaseAddress!, csvUrlOrPath.TrimStart('/'));
    }

    public void Dispose()
    {
        _client.Dispose();
    }

    private sealed record ProjectInfo(int Id, string Name, string Identifier);

    private sealed record IssueInfo(string ProjectName, IReadOnlyList<string> AllowedStatuses);

    private sealed record CustomFieldInfo(int Id, string Name, string FieldFormat, bool AllowsMultiple, IReadOnlyList<string> PossibleValues);
}

internal sealed record RedmineCellConstraint(IReadOnlyList<string> Options, bool AllowsMultiple);
internal sealed record RedmineProjectChoice(string Name, string Identifier);
internal sealed record RedmineIssueComment(int IssueId, int CommentId, string AuthorName, string CreatedOn, string Notes);
internal sealed record CreatedIssueInfo(int IssueId, IReadOnlyDictionary<string, string> Fields);