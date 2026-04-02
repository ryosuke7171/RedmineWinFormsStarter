using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualBasic.FileIO;

namespace RedmineWinFormsStarter;

internal static class CsvLoader
{
    // LINKTAG RMCSV001
    private static readonly Encoding Utf8Encoding = new UTF8Encoding(encoderShouldEmitUTF8Identifier: true);
    private static readonly Encoding Cp932Encoding = Encoding.GetEncoding(932);

    public static DataTable Load(string path)
    {
        ArgumentNullException.ThrowIfNull(path);

        return Load(File.ReadAllBytes(path));
    }

    public static DataTable Load(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        var encoding = DetectEncoding(content);
        using var stream = new MemoryStream(content, writable: false);
        using var reader = new StreamReader(stream, encoding, detectEncodingFromByteOrderMarks: true);
        return Load(reader, encoding);
    }

    public static Encoding DetectEncoding(byte[] content)
    {
        ArgumentNullException.ThrowIfNull(content);

        if (HasUtf8Bom(content))
        {
            return Utf8Encoding;
        }

        var utf8Strict = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
        try
        {
            _ = utf8Strict.GetString(content);
            return Utf8Encoding;
        }
        catch (DecoderFallbackException)
        {
            return Cp932Encoding;
        }
    }

    public static string GetEncodingDisplayName(Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(encoding);

        return encoding.CodePage == 932 ? "CP932" : encoding.WebName.ToUpperInvariant();
    }

    public static byte[] SaveToBytes(DataTable table)
    {
        ArgumentNullException.ThrowIfNull(table);

        using var stream = new MemoryStream();
        using (var writer = new StreamWriter(stream, Utf8Encoding, leaveOpen: true))
        {
            WriteTable(writer, table);
        }

        table.AcceptChanges();
        return stream.ToArray();
    }

    public static void Save(string path, DataTable table)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(table);

        using var writer = new StreamWriter(path, false, Utf8Encoding);
        WriteTable(writer, table);
        table.AcceptChanges();
    }

    private static DataTable Load(TextReader reader, Encoding encoding)
    {
        ArgumentNullException.ThrowIfNull(reader);
        ArgumentNullException.ThrowIfNull(encoding);

        // Robust CSV: skip leading blank lines, allow duplicate headers.
        using var parser = new TextFieldParser(reader);
        parser.TextFieldType = FieldType.Delimited;
        parser.SetDelimiters(",");
        parser.HasFieldsEnclosedInQuotes = true;

        string[]? header = null;
        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields == null) continue;
            if (fields.Any(f => !string.IsNullOrWhiteSpace(f))) { header = fields; break; }
        }
        if (header == null) throw new InvalidDataException("CSV header not found.");

        var normalizedHeaders = header
            .Select(value => (value ?? string.Empty).Trim().TrimStart('﻿'))
            .ToArray();

        var (displayHeaders, columnNames) = MakeUniqueHeaders(normalizedHeaders);

        var dt = new DataTable();
        for (int i = 0; i < columnNames.Count; i++)
        {
            var column = dt.Columns.Add(columnNames[i]);
            column.Caption = displayHeaders[i];
        }

        while (!parser.EndOfData)
        {
            var fields = parser.ReadFields();
            if (fields == null) continue;
            var row = dt.NewRow();
            for (int i = 0; i < columnNames.Count; i++)
            {
                row[i] = i < fields.Length ? (fields[i] ?? "") : "";
            }
            dt.Rows.Add(row);
        }

        // stash display headers in ExtendedProperties (for optional Save feature)
        dt.ExtendedProperties["DisplayHeaders"] = displayHeaders;
        dt.ExtendedProperties["ColumnNames"] = columnNames;
        dt.ExtendedProperties["DetectedEncoding"] = GetEncodingDisplayName(encoding);
        return dt;
    }

    private static (List<string> displayHeaders, List<string> columnNames) MakeUniqueHeaders(string[] headers)
    {
        var display = new List<string>(headers.Length);
        var cols = new List<string>(headers.Length);
        var seen = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        int dateIdx = 0;
        foreach (var raw0 in headers)
        {
            var raw = (raw0 ?? "").Trim();
            if (string.IsNullOrWhiteSpace(raw)) raw = "col";

            // optional normalization: date-like header -> date_#
            var save = raw;
            if (System.Text.RegularExpressions.Regex.IsMatch(raw, @"^\d{4}[/-]\d{2}[/-]\d{2}$"))
            {
                dateIdx++;
                save = $"date_{dateIdx}";
            }

            var key = save;
            if (!seen.TryGetValue(key, out var n))
            {
                seen[key] = 1;
                cols.Add(save);
            }
            else
            {
                n++;
                seen[key] = n;
                cols.Add($"{save}_{n}");
            }
            display.Add(raw);
        }
        return (display, cols);
    }

    private static IReadOnlyList<string> GetDisplayHeaders(DataTable table)
    {
        if (table.ExtendedProperties["DisplayHeaders"] is List<string> displayHeaders && displayHeaders.Count == table.Columns.Count)
        {
            return displayHeaders;
        }

        return table.Columns.Cast<DataColumn>().Select(column => column.Caption ?? column.ColumnName).ToArray();
    }

    private static string Escape(string value)
    {
        if (value.IndexOfAny([',', '"', '\r', '\n']) < 0)
        {
            return value;
        }

        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private static void WriteTable(TextWriter writer, DataTable table)
    {
        ArgumentNullException.ThrowIfNull(writer);
        ArgumentNullException.ThrowIfNull(table);

        var headers = GetDisplayHeaders(table);
        writer.WriteLine(string.Join(",", headers.Select(Escape)));

        foreach (DataRow row in table.Rows)
        {
            if (row.RowState == DataRowState.Deleted)
            {
                continue;
            }

            var fields = table.Columns
                .Cast<DataColumn>()
                .Select(column => row[column] == DBNull.Value ? string.Empty : Convert.ToString(row[column]) ?? string.Empty);

            writer.WriteLine(string.Join(",", fields.Select(Escape)));
        }
    }

    private static bool HasUtf8Bom(byte[] content)
        => content.Length >= 3
            && content[0] == 0xEF
            && content[1] == 0xBB
            && content[2] == 0xBF;
}
