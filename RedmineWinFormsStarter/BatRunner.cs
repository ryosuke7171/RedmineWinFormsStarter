using System;
using System.Diagnostics;
using System.IO;

namespace RedmineWinFormsStarter;

internal static class BatRunner
{
    // LINKTAG RMBAT003
    public static Process Start(string batFileName, string redmineUrl, string apiKey, string csvPath,
        Action<string> onLine)
    {
        var exeDir = AppContext.BaseDirectory;
        var batPath = Path.Combine(exeDir, batFileName);
        if (!File.Exists(batPath))
            throw new FileNotFoundException($"BAT not found: {batPath}");

        var psi = new ProcessStartInfo
        {
            FileName = batPath,
            Arguments = $"\"{redmineUrl}\" \"{apiKey}\" \"{csvPath}\"",
            WorkingDirectory = exeDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true
        };

        var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
        p.OutputDataReceived += (_, e) => { if (e.Data != null) onLine(e.Data); };
        p.ErrorDataReceived += (_, e) => { if (e.Data != null) onLine(e.Data); };
        p.Exited += (_, __) => onLine($"[EXIT] {p.ExitCode}");

        p.Start();
        p.BeginOutputReadLine();
        p.BeginErrorReadLine();
        return p;
    }
}