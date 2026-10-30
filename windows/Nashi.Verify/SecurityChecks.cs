using System.IO.Compression;
using System.Text;
using Nashi.Core;
// Link validation for web downloads and external-reference stripping for Office packages.
internal static class SecurityChecks
{
    internal static void Run(Action<bool,string> check,string root)
    {
        foreach(var blocked in new[]{"http://169.254.169.254/latest/meta-data/","http://192.168.1.10/video.mp4","http://10.0.0.5/x","http://172.16.0.1/x","http://172.31.255.255/x","http://100.64.0.1/x","http://0.0.0.0/x","http://[::1]/x","http://[fd00::1]/x","http://[fe80::1]/x","http://[::ffff:192.168.0.1]/x","http://intranet/video","http://nas.local/x","http://printer.lan/x","http://localhost/x","http://127.0.0.1:8080/x"})
        {
            var refused=false;try{WebDownload.Validate(blocked);}catch(ArgumentException){refused=true;}
            check(refused,"A private or local link was accepted: "+blocked);
        }
        foreach(var allowed in new[]{"https://www.youtube.com/watch?v=abc","https://example.com/a.mp4","http://93.184.216.34/a.mp4","https://[2606:2800:220:1:248:1893:25c8:1946]/a","http://172.32.0.1/x","http://100.128.0.1/x"})
        {
            var accepted=true;try{WebDownload.Validate(allowed);}catch(ArgumentException){accepted=false;}
            check(accepted,"A public link was refused: "+allowed);
        }
        var package=Path.Combine(root,"external-reference.docx");
        const string rels="<?xml version=\"1.0\"?><Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">"
            +"<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>"
            +"<Relationship Id=\"rId2\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/image\" Target=\"file:///C:/Users/me/secret.png\" TargetMode=\"External\"/>"
            +"<Relationship Id=\"rId3\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/hyperlink\" Target=\"https://example.com/\" TargetMode=\"External\"/>"
            +"<Relationship Id=\"rId4\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/oleObject\" Target=\"\\\\server\\share\\a.bin\" TargetMode=\"External\"/></Relationships>";
        using(var zip=ZipFile.Open(package,ZipArchiveMode.Create))
        {
            foreach(var (name,text) in new[]{("word/document.xml","<w:document xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"/>"),("word/_rels/document.xml.rels",rels)})
            {using var writer=new StreamWriter(zip.CreateEntry(name).Open(),new UTF8Encoding(false));writer.Write(text);}
        }
        var removed=OoxmlSanitizer.StripExternalTargets(package);
        using(var zip=ZipFile.OpenRead(package))
        {
            string Read(string name){using var reader=new StreamReader(zip.GetEntry(name)!.Open());return reader.ReadToEnd();}
            var cleaned=Read("word/_rels/document.xml.rels");
            check(removed==2&&!cleaned.Contains("secret.png")&&!cleaned.Contains("server")&&cleaned.Contains("example.com")&&cleaned.Contains("styles.xml"),"External Office references were not removed, or a hyperlink/internal part was lost");
            check(Read("word/document.xml").Contains("w:document"),"Sanitizing changed an unrelated package part");
        }
        var notPackage=Path.Combine(root,"legacy.docx");File.WriteAllText(notPackage,"{\\rtf1 not a zip}");
        check(OoxmlSanitizer.StripExternalTargets(notPackage)==0&&File.ReadAllText(notPackage).StartsWith("{\\rtf1"),"A file that is not an Office package was modified");
    }
}
