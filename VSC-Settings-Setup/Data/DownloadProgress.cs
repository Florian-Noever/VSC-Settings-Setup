namespace VSC_Settings_Setup.Data;

public readonly record struct DownloadProgress(long BytesReceived, long? TotalBytes);
