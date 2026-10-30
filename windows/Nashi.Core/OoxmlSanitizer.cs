using System.IO.Compression;
using System.Xml;
using System.Xml.Linq;
namespace Nashi.Core;

/// Office conversion hands a document from someone else to LibreOffice, which follows external relationship targets such
/// as file:///C:/Users/me/secret.png and embeds the file it finds into the converted copy. Stripping every external
/// relationship except hyperlinks keeps the conversion limited to what is inside the package.
internal static class OoxmlSanitizer
{
    private const string RelationshipNamespace="http://schemas.openxmlformats.org/package/2006/relationships";
    /// Edits the package in place and returns how many external references were removed. A file that is not a ZIP
    /// package is left alone (the legacy binary formats have no relationship parts).
    internal static int StripExternalTargets(string package)
    {
        ZipArchive zip;
        try{zip=ZipFile.Open(package,ZipArchiveMode.Update);}catch(InvalidDataException){return 0;}
        var removed=0;
        using(zip)
        {
            foreach(var entry in zip.Entries.Where(e=>e.FullName.EndsWith(".rels",StringComparison.OrdinalIgnoreCase)).ToArray())
            {
                if(entry.Length>20_000_000)throw new InvalidDataException("The document has an oversized relationship part, so it was not converted.");
                XDocument document;
                try
                {
                    using var stream=entry.Open();using var reader=XmlReader.Create(stream,new(){DtdProcessing=DtdProcessing.Prohibit,XmlResolver=null,MaxCharactersInDocument=20_000_000});document=XDocument.Load(reader);
                }
                catch(XmlException error){throw new InvalidDataException("The document's relationships could not be checked, so it was not converted.",error);}
                var external=document.Descendants(XName.Get("Relationship",RelationshipNamespace)).Where(r=>string.Equals((string?)r.Attribute("TargetMode"),"External",StringComparison.OrdinalIgnoreCase)&&!((string?)r.Attribute("Type")??"").EndsWith("/hyperlink",StringComparison.OrdinalIgnoreCase)).ToList();
                if(external.Count==0)continue;
                foreach(var relationship in external)relationship.Remove();
                removed+=external.Count;var name=entry.FullName;entry.Delete();
                using var output=zip.CreateEntry(name).Open();document.Save(output);
            }
        }
        return removed;
    }
}
