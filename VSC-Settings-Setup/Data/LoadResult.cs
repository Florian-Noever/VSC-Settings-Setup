namespace VSC_Settings_Setup.Data;

public sealed record LoadResult<T>(T Value, IReadOnlyList<string> Warnings);
