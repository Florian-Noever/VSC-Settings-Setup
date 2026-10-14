using System.Globalization;

using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.TypeConversion;

using VSC_Settings_Setup.Data;

namespace VSC_Settings_Setup.Services;

public static class ExtensionListReader
{
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
            ReadingExceptionOccurred = args =>
            {
                warnings.Add($"Skipped line {args.Exception.Context?.Parser?.RawRow}: {Describe(args.Exception)}");
                return false;
            },
        };

        using var csv = new CsvReader(reader, configuration);
        csv.Context.RegisterClassMap(new VSCodeExtensionMap());

        var extensions = new List<VSCodeExtension>();
        var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            foreach (var extension in csv.GetRecords<VSCodeExtension>())
            {
                var line = csv.Parser.RawRow;
                if (linesWithInvalidQuotes.Contains(line))
                    warnings.Add($"Skipped line {line}: A field contains invalid quotes.");
                else if (!VSCodeExtension.IsValidId(extension.Id))
                    warnings.Add($"Skipped line {line}: '{extension.Id}' is not a valid extension ID.");
                else if (!ids.Add(extension.Id))
                    warnings.Add($"Skipped line {line}: '{extension.Id}' is listed more than once.");
                else
                    extensions.Add(extension);
            }
        }
        catch (HeaderValidationException ex)
        {
            var missingColumns = string.Join(", ", ex.InvalidHeaders.SelectMany(header => header.Names));
            throw new InvalidDataException($"Required column(s) missing: {missingColumns}.", ex);
        }
        catch (CsvHelperException ex)
        {
            throw new InvalidDataException($"Line {ex.Context?.Parser?.RawRow} could not be read. {Describe(ex)}", ex);
        }

        return new(extensions, warnings);
    }

    private static string Describe(CsvHelperException exception) => exception switch
    {
        TypeConverterException ex => $"'{ex.Text}' is not a valid value for {ex.MemberMapData?.Names.FirstOrDefault()}.",
        CsvHelper.MissingFieldException => "A field is missing.",
        _ => "The line has an invalid format.",
    };

    private sealed class VSCodeExtensionMap : ClassMap<VSCodeExtension>
    {
        public VSCodeExtensionMap()
        {
            Map(extension => extension.Name).Name("name");
            Map(extension => extension.DisplayName).Name("displayname").Optional();
            Map(extension => extension.Publisher).Name("publisher");
            Map(extension => extension.Author).Name("author").Optional();
            Map(extension => extension.Description).Name("description").Optional();
            Map(extension => extension.PreRelease).Name("prerelease").Optional().Default(false);
        }
    }
}
