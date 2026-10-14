using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;

using VSC_Settings_Setup.Data;

namespace VSC_Settings_Setup.Services;

public static class ExtensionListReader
{
    private const string NameColumn = "name";
    private const string PublisherColumn = "publisher";
    private const string DisplayNameColumn = "displayname";
    private const string AuthorColumn = "author";
    private const string DescriptionColumn = "description";
    private const string PreReleaseColumn = "prerelease";
    private static readonly string[] RequiredColumns = [NameColumn, PublisherColumn];
    private static readonly string[] OptionalColumns = [DisplayNameColumn, AuthorColumn, DescriptionColumn, PreReleaseColumn];

    /// <exception cref="InvalidDataException">The file is not a valid extension list.</exception>
    public static LoadResult<IReadOnlyList<VSCodeExtension>> Read(string path)
    {
        using var reader = File.OpenText(path);
        return Read(reader);
    }

    /// <summary>Reads an extension list, skipping invalid or duplicate lines with a warning.</summary>
    /// <exception cref="InvalidDataException">The CSV is not a valid extension list.</exception>
    public static LoadResult<IReadOnlyList<VSCodeExtension>> Read(TextReader reader)
    {
        var warnings = new List<string>();
        var linesWithInvalidQuotes = new HashSet<int>();
        var configuration = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            PrepareHeaderForMatch = args => args.Header.Trim().ToLowerInvariant(),
            TrimOptions = TrimOptions.Trim,
            // Throwing on bad data (the default) leaves the parser in a broken state for the following lines, so they are only recorded and skipped below.
            BadDataFound = args => linesWithInvalidQuotes.Add(args.Context.Parser!.RawRow),
        };

        using var csv = new CsvReader(reader, configuration);
        var extensions = new List<VSCodeExtension>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            if (!csv.Read())
                return new(extensions, warnings);

            csv.ReadHeader();
            var columns = FindColumns(csv);
            while (csv.Read())
            {
                var line = csv.Parser.RawRow;
                if (!TryReadExtension(csv, columns, out var extension, out var error))
                    warnings.Add($"Skipped line {line}: {error}");
                else if (linesWithInvalidQuotes.Contains(line))
                    warnings.Add($"Skipped line {line}: A field contains invalid quotes.");
                else if (!VSCodeExtension.IsValidId(extension.Id))
                    warnings.Add($"Skipped line {line}: '{extension.Id}' is not a valid extension ID.");
                else if (!ids.Add(extension.Id))
                    warnings.Add($"Skipped line {line}: '{extension.Id}' is listed more than once.");
                else
                    extensions.Add(extension);
            }
        }
        catch (CsvHelperException ex)
        {
            throw new InvalidDataException($"Line {ex.Context?.Parser?.RawRow} could not be read. The line has an invalid format.", ex);
        }

        return new(extensions, warnings);
    }

    // Columns are read by name instead of through a CsvHelper class map, whose reflection breaks once the app is compiled with Native AOT.
    private static Dictionary<string, int> FindColumns(CsvReader csv)
    {
        var columns = RequiredColumns.Concat(OptionalColumns).ToDictionary(column => column, column => csv.GetFieldIndex(column, isTryGet: true));
        var missingColumns = RequiredColumns.Where(column => columns[column] < 0).ToList();
        if (missingColumns.Count > 0)
            throw new InvalidDataException($"Required column(s) missing: {string.Join(", ", missingColumns)}.");
        return columns;
    }

    private static bool TryReadExtension(CsvReader csv, Dictionary<string, int> columns, [NotNullWhen(true)] out VSCodeExtension? extension, [NotNullWhen(false)] out string? error)
    {
        extension = null;
        try
        {
            var name = Field(NameColumn);
            var displayName = Field(DisplayNameColumn);
            var publisher = Field(PublisherColumn);
            var author = Field(AuthorColumn);
            var description = Field(DescriptionColumn);
            var preReleaseText = Field(PreReleaseColumn);
            var preRelease = false;
            if (preReleaseText.Length > 0 && !csv.TryGetField(columns[PreReleaseColumn], out preRelease))
            {
                error = $"'{preReleaseText}' is not a valid value for {PreReleaseColumn}.";
                return false;
            }

            extension = new VSCodeExtension { Name = name, DisplayName = displayName, Publisher = publisher, Author = author, Description = description, PreRelease = preRelease };
            error = null;
            return true;
        }
        catch (CsvHelper.MissingFieldException)
        {
            error = "A field is missing.";
            return false;
        }

        string Field(string column) => columns[column] < 0 ? string.Empty : csv.GetField(columns[column]) ?? string.Empty;
    }
}
