using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace PubSub.Application.Api.UserSync.Beskedfordeler;

public sealed class BeskedfordelerXmlReader(XmlSchemaSet schemas)
{
    public const int MaxBodySize = 1024 * 1024;

    // Load all schemas explicitly. Neither imports nor incoming XML may resolve files or URLs.
    public static XmlSchemaSet LoadSchemas(string directory)
    {
        var schemas = new XmlSchemaSet { XmlResolver = null };
        schemas.ValidationEventHandler += (_, args) => throw new XmlSchemaException(args.Message);
        foreach (var file in Directory.EnumerateFiles(directory, "*.xsd", SearchOption.AllDirectories))
        {
            using var reader = XmlReader.Create(file, new XmlReaderSettings
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
