# FlipFlop

A small Windows app that converts and edits your files on your own computer: images, PDFs, documents, audio, video and archives. Drag a file, pick what you want from a grid of colourful tiles, done. Nothing is uploaded anywhere.

FlipFlop is a modified version of **[Nashi](https://github.com/orkunisitmak/nashi-app)** by Orkun Işıtmak (MIT). Nashi was itself inspired by Tangerine's drag-and-wheel idea. FlipFlop has its own name, logo, colours, layout and tool grid, and several fixes (see below). It is not affiliated with Nashi, Tangerine or LEAP STUDIO.

## What it does
- **Convert** between 45 formats: PNG, JPG, WEBP, HEIC, AVIF, TIFF, BMP, SVG, PDF, DOCX, TXT, MD, EPUB, XLSX, CSV, MP4, MKV, WEBM, MOV, AVI, MP3, WAV, FLAC and more.
- **Tools:** compress, crop, annotate, redact, add a background, collage, edit metadata, read QR codes, merge/split/organize PDFs, trim/speed up/split/mute video, trim/normalize audio, extract archives.
- **Download** public videos, audio and images from a link (needs yt-dlp, installed on first run).
- Turkish and English interface, light and dark theme, keyboard and screen-reader friendly.

## Download (Windows 10/11, 64-bit)
Get **FlipFlop-Windows-x64.zip** from the [latest release](../../releases/latest); the SHA-256 checksum is next to it.
1. Extract the **whole** ZIP into its own folder (do not run `FlipFlop.exe` from inside the ZIP).
2. Open `FlipFlop.exe`. Windows SmartScreen may warn because the app is not code-signed: choose **More info → Run anyway**.
3. If **Smart App Control** is on (Windows Security → App & browser control), Windows refuses unsigned apps outright and FlipFlop will not start. Only turning Smart App Control off allows it, and Windows cannot turn it back on without a reset. Leave it on if you are unsure; a signed build is needed for those PCs.

## Using it
1. Drop a file on the window, press **Choose files**, or paste (`Ctrl+V`).
2. Press **Open wheel** (or `Ctrl+Space`) and choose a tile. Dragging from Explorer works too: hold **Shift** (convert), **Ctrl** (tools) or **Alt** (file actions) while you drag.
3. New files are saved next to the original unless you choose another folder in Settings.

First launch offers to download FFmpeg, yt-dlp, Deno and LibreOffice (about 540 MB) directly from their official sources, checked against pinned SHA-256 hashes. Image, PDF and ZIP tools work without them.

## Build (Windows 10/11 x64)
```powershell
# needs the .NET 10 SDK
dotnet publish windows/Nashi.Windows -c Release -o build/windows/FlipFlop
build\windows\FlipFlop\FlipFlop.exe
```
The project folders and namespaces still say `Nashi` internally on purpose, so changes from upstream stay easy to compare. Checks: `dotnet run --project windows/Nashi.Verify -c Release` (set `NASHI_VERIFY_TOOLS_DIRECTORY` to the tools folder).

## Known limits
- **Builds are not code-signed.** Windows SmartScreen warns, and **Smart App Control blocks unsigned apps** on machines where it is on. Signing is needed before sharing builds widely.
- Automatic updates are switched off in this fork (the update feed and signing key belong to upstream).
- Only the Windows app is maintained here. The macOS (Swift) sources are inherited from Nashi, are not built or tested, and no longer ship with artwork.
- HTML files cannot be converted yet. ODF/RTF/legacy `.doc`/`.xls` files are not cleaned of external references before conversion.
- Not hand-tested: Ctrl/Alt drag wheels, window-placement wheel, drawing tools (redact/annotate), tray icon, floating shelf.

## What changed from Nashi
- New identity: name, logo, palette, sidebar layout, tile-grid wheel instead of a ring.
- **Fixes:** image conversion failed when the Windows user path contained Turkish letters (exiftool); Split/Organize PDF crashed with a cross-thread error; "Show in folder" opened Documents instead of selecting the file; Library search now ignores Turkish accents (seker = şeker); clearer, translated error messages; many unlabeled controls named for screen readers.
- **Security:** external file/URL references are stripped from DOCX/PPTX/XLSX before LibreOffice sees them (stops local-file inclusion); web download refuses private, link-local and intranet addresses.

## License
MIT. See [LICENSE](LICENSE): the original Nashi copyright stays, FlipFlop changes are © Burhan Enes Karataş. Bundled and downloaded tools keep their own licenses, see [docs/dependencies.md](docs/dependencies.md) and the `THIRD-PARTY-NOTICES` files.
