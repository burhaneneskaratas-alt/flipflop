using System.Text.Json;
namespace Nashi.Core;
public sealed partial class WebDownload(Toolchain tools)
{
    public static Uri Validate(string input)
    {
        if(!Uri.TryCreate(input,UriKind.Absolute,out var url)||url.Scheme is not ("http" or "https")||!string.IsNullOrEmpty(url.UserInfo)||NonPublicHost(url))throw new ArgumentException("Paste a public http(s) media or page link.");return url;
    }
    // A pasted link must not make yt-dlp or the image fetcher call into the user's own network or a cloud metadata
    // service (169.254.169.254). IP literals are checked by range; bare and intranet-style names are refused outright.
    // A public name that resolves to a private address is still reachable here, since DNS is not consulted.
    // A public-looking name can still point at a private address, so resolve it first. The download tool resolves it again
    // a moment later, which means this narrows the gap rather than closing it.
    internal static async Task EnsurePublicAsync(Uri url,CancellationToken token)
    {
        if(System.Net.IPAddress.TryParse(url.DnsSafeHost,out _))return;
        System.Net.IPAddress[] addresses;
        try{addresses=await System.Net.Dns.GetHostAddressesAsync(url.DnsSafeHost,token);}
        catch(System.Net.Sockets.SocketException){return;} // an unknown name is reported by the download tool itself
        if(addresses.Any(NonPublicAddress))throw new ArgumentException("Paste a public http(s) media or page link.");
    }
    internal static bool NonPublicHost(Uri url)
    {
        var host=url.DnsSafeHost.TrimEnd('.');if(host.Length==0||url.IsLoopback)return true;
        var scope=host.IndexOf('%');if(scope>=0)host=host[..scope];
        if(System.Net.IPAddress.TryParse(host,out var address))return NonPublicAddress(address);
        if(!host.Contains('.'))return true;
        foreach(var suffix in new[]{".local",".localhost",".internal",".lan",".home.arpa",".intranet",".corp"})if(host.EndsWith(suffix,StringComparison.OrdinalIgnoreCase))return true;
        return false;
    }
    internal static bool NonPublicAddress(System.Net.IPAddress address)
    {
        if(address.IsIPv4MappedToIPv6)address=address.MapToIPv4();
        if(System.Net.IPAddress.IsLoopback(address))return true;var b=address.GetAddressBytes();
        if(address.AddressFamily==System.Net.Sockets.AddressFamily.InterNetwork)
            return b[0] is 0 or 10 or 127||b[0]>=224||(b[0]==100&&b[1] is >=64 and <=127)||(b[0]==169&&b[1]==254)||(b[0]==172&&b[1] is >=16 and <=31)||(b[0]==192&&b[1]==168)||(b[0]==192&&b[1]==0&&b[2]==0)||(b[0]==198&&b[1] is 18 or 19);
        return address.Equals(System.Net.IPAddress.IPv6Any)||address.IsIPv6LinkLocal||address.IsIPv6SiteLocal||address.IsIPv6Multicast||(b[0]&0xFE)==0xFC;
    }
    public async Task<IReadOnlyList<string>> DownloadAsync(string input,string kind,string destination,IProgress<OperationProgress>? progress=null,CancellationToken token=default,int? maximumHeight=null,IReadOnlySet<int>? selectedPhotos=null)
    {
        if(maximumHeight is <2 or >16384)throw new ArgumentException("Choose a supported maximum video height.");
        var payload=WebMediaPayload.Parse(input);var uri=Validate(payload?.Url??input);await EnsurePublicAsync(uri,token);if(kind is not ("video" or "audio" or "thumbnail" or "photos" or "image"))throw new ArgumentException("Choose video, audio, thumbnail or images.");
        using var tx=new ExportTransaction(destination);
        if(kind=="image"||payload?.Kind=="image")return tx.Publish([await WebPhotos.DownloadImageAsync(uri.AbsoluteUri,tx.StagingDirectory,progress,token)],token);
        if(kind=="thumbnail"&&WebMediaPayload.YoutubeId(uri) is {} id)return tx.Publish([await WebPhotos.YoutubeThumbnail(id,tx.StagingDirectory,progress,token)],token);
        if(kind=="photos")
        {
            var photos=await InspectPhotosAsync(uri.AbsoluteUri,token);if(selectedPhotos is not null){if(selectedPhotos.Count==0||selectedPhotos.Any(id=>!photos.Any(p=>p.Id==id)))throw new ArgumentException("Select at least one available photo.");photos=photos.Where(p=>selectedPhotos.Contains(p.Id)).ToArray();}var outputs=new List<string>();var n=0;foreach(var photo in photos){var local=new ProgressScope(progress,n++,photos.Length);outputs.Add(await WebPhotos.DownloadImageAsync(photo.Url,tx.StagingDirectory,local,token,"Instagram "+uri.Segments.Last().Trim('/')+" "+photo.Id.ToString("00")));}return tx.Publish(outputs,token);
        }
        var args=new List<string>{"--ignore-config","--no-playlist","--no-part","--no-overwrites","--no-cache-dir","--restrict-filenames","--no-mtime","--progress","--newline","--progress-template","download:NASHI:%(progress.downloaded_bytes)s:%(progress.total_bytes,progress.total_bytes_estimate)s","--print","after_move:filepath","--ffmpeg-location",tools.DirectoryPath,"--js-runtimes","deno:"+tools.Executable("deno"),"-P",tx.StagingDirectory,"-o","%(title).160B [%(id)s].%(ext)s"};
        if(kind=="audio")args.AddRange(["-x","--audio-format","mp3"]);else if(kind=="thumbnail")args.AddRange(["--skip-download","--write-thumbnail","--convert-thumbnails","jpg"]);else args.AddRange(["-f",maximumHeight is {} height?$"bv*[height<={height}]+ba/b[height<={height}]/bv*+ba/b":"bv*+ba/b","--merge-output-format","mp4"]);
        args.AddRange(["--",uri.AbsoluteUri]);
        progress?.Report(new(null,"Resolving media…"));
        await ProcessRunner.RunAsync(tools.Executable("yt-dlp"),args,token,line=>
        {
            if(!line.StartsWith("NASHI:",StringComparison.Ordinal))return;
            var parts=line.Split(':');if(parts.Length==3&&double.TryParse(parts[1],out var current)&&double.TryParse(parts[2],out var total)&&total>0)progress?.Report(new(Math.Clamp(current/total,0,.98),"Downloading media…"));
        });
        var files=Directory.GetFiles(tx.StagingDirectory).Where(p=>new FileInfo(p).Length>0).ToList();
        if(files.Count==0)throw new IOException("No public downloadable media found. Some sites require sign-in or do not expose downloadable media.");
        if(kind=="video"&&maximumHeight is {} limit)
        {
            var engine=new MediaEngine(tools.Executable("ffmpeg"),tools.Executable("ffprobe"));for(var n=0;n<files.Count;n++){var file=files[n];var info=await engine.InspectAsync(file,token);if(info.Video is null)throw new InvalidDataException("The downloaded file contains no video.");if(info.Video.Height>limit){var resized=await engine.ConvertAsync([file],"mp4",tx.StagingDirectory,new(){Height=limit},progress,token);File.Delete(file);files[n]=resized.Single();}}
        }
        progress?.Report(new(.99,"Saving files…"));return tx.Publish(files,token);
    }
    public async Task<WebPhoto[]> InspectPhotosAsync(string input,CancellationToken token=default)
    {
        var uri=Validate(input);if(WebMediaPayload.Parse(input)?.Kind!="instagramPhotos")throw new ArgumentException("Choose an Instagram photo post link.");var result=await ProcessRunner.RunAsync(tools.Executable("yt-dlp"),["--ignore-config","--no-cache-dir","--yes-playlist","--playlist-end","51","--ignore-no-formats-error","--skip-download","--dump-single-json","--no-progress","--",uri.AbsoluteUri],token);return WebPhotos.Instagram(result.Output);
    }
    private sealed class ProgressScope(IProgress<OperationProgress>? progress,int index,int total):IProgress<OperationProgress>{public void Report(OperationProgress value)=>progress?.Report(value with{Fraction=value.Fraction is {} n?(index+n)/total*.98:null,Label=$"Photo {index+1} of {total}…"});}
}
