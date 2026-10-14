using VSC_Settings_Setup.Data;

namespace VSC_Settings_Setup.Services;

public static class HttpClientExtensions
{
    public static async Task DownloadFileAsync(this HttpClient httpClient, string url, string filePath, IProgress<DownloadProgress>? progress = null)
    {
        // ResponseHeadersRead streams the body instead of buffering it, which also keeps large downloads out of HttpClient.Timeout.
        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(filePath))!);
        var totalBytes = response.Content.Headers.ContentLength;
        await using var source = await response.Content.ReadAsStreamAsync();
        await using var target = File.Create(filePath);

        var buffer = new byte[81920];
        long bytesReceived = 0;
        int bytesRead;
        while ((bytesRead = await source.ReadAsync(buffer)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, bytesRead));
            bytesReceived += bytesRead;
            progress?.Report(new DownloadProgress(bytesReceived, totalBytes));
        }
    }
}
