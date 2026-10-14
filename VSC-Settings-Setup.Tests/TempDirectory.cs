namespace VSC_Settings_Setup.Tests;

internal sealed class TempDirectory : IDisposable
{
    public string Path { get; } = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "VSC-Settings-Setup.Tests", Guid.NewGuid().ToString("N"));

    public TempDirectory() => Directory.CreateDirectory(Path);

    public void WriteFile(string fileName, string content) => File.WriteAllText(System.IO.Path.Combine(Path, fileName), content);

    public void Dispose() => Directory.Delete(Path, recursive: true);
}
