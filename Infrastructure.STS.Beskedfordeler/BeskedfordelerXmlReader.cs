using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Infrastructure.STS.Beskedfordeler;

public sealed class BeskedfordelerXmlReader(XmlSchemaSet schemas)
{
    public const int MaxBodySize = 1024 * 1024;

    // Load all schemas explicitly. Neither imports nor incoming XML may resolve files or URLs.
    public static XmlSchemaSet LoadSchemas(string? directory = null)
    {
        var schemas = new XmlSchemaSet { XmlResolver = null };
        schemas.ValidationEventHandler += (_, args) => throw new XmlSchemaException(args.Message);
        var assembly = typeof(BeskedfordelerXmlReader).Assembly;
        var sources = directory == null
            ? assembly.GetManifestResourceNames().Where(name =>
                name.StartsWith("Infrastructure.STS.Beskedfordeler.Schemas.", StringComparison.Ordinal) &&
                name.EndsWith(".xsd", StringComparison.Ordinal))
            : Directory.EnumerateFiles(directory, "*.xsd", SearchOption.AllDirectories);
        foreach (var source in sources)
        {
            using var stream = directory == null
                ? assembly.GetManifestResourceStream(source) ??
                  throw new InvalidOperationException($"Missing embedded Beskedfordeler schema: {source}")
                : File.OpenRead(source);
            using var reader = XmlReader.Create(stream, new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null
            });
            schemas.Add(null, reader);
        }
        schemas.Compile();
        if (!schemas.GlobalElements.Contains(new XmlQualifiedName("ModtagBeskedInput", BeskedfordelerResponse.Sts.NamespaceName)))
            throw new XmlSchemaException("The complete Beskedfordeler callback schema set is required.");
        return schemas;
    }

    public async Task<XElement> Read(Stream body, CancellationToken cancellationToken)
    {
        var settings = new XmlReaderSettings
        {
            Async = true,
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = MaxBodySize,
            ValidationType = ValidationType.Schema,
            Schemas = schemas,
            ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings |
                              XmlSchemaValidationFlags.ProcessIdentityConstraints
        };
        settings.ValidationEventHandler += (_, args) => throw new XmlSchemaValidationException(args.Message);
        using var reader = XmlReader.Create(body, settings);
        var document = await XDocument.LoadAsync(reader, LoadOptions.None, cancellationToken);
        if (document.Root?.Name != BeskedfordelerResponse.Sts + "ModtagBeskedInput")
            throw new XmlSchemaValidationException("Expected ModtagBeskedInput.");
        return document.Root;
    }
}
