using System.Text.RegularExpressions;
namespace Nashi.Core;

// Names and endpoints that identify this build. Rename the app by changing them here.
public static class Brand
{
    // The name the inherited UI strings and Turkish catalog were written with.
    private const string InheritedName="Nashi";
    public const string AppName="FlipFlop";
    public const string ExecutableName=AppName+".exe";
    public const string LibraryFileName=AppName+".dll";
    // Folder under %LOCALAPPDATA% for the Library, preferences and downloaded tools.
    public const string DataFolderName=AppName;
    // Single-instance mutex/pipe prefix. Distinct from upstream so both builds can run side by side.
    public const string InstanceName=AppName;
    // A static readonly field, not a const, so disabled-update branches do not trip warnings-as-errors.
    // Updates come from this fork's own releases and are signed with its own key (see PublicUpdates).
    public static readonly bool UpdatesEnabled=true;
    public static string DataRoot=>Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),DataFolderName);
    public static string ToolsRoot=>Path.Combine(DataRoot,"Tools");

    // Turkish case suffixes follow the last vowel of the name, so the inherited "Nashi'ye" must become "FlipFlop'a".
    // Revisit this table whenever AppName changes.
    private static readonly Dictionary<string,string> TurkishSuffixes=new(){["ye"]="a",["yi"]="u",["de"]="ta",["den"]="tan",["nin"]="un"};
    private static readonly Regex Suffixed=new(@"\bNashi(['’])(ye|yi|den|de|nin)\b",RegexOptions.CultureInvariant);
    // The same suffixes in the upper-case wheel/button labels ("NASHI'Yİ" must become "FLIPFLOP'U").
    private static readonly Dictionary<string,string> TurkishCapitalSuffixes=new(){["YE"]="A",["Yİ"]="U",["YI"]="U",["DE"]="TA",["DEN"]="TAN",["NİN"]="UN",["NIN"]="UN"};
    private static readonly Regex SuffixedCapitals=new(@"\bNASHI(['’])(YE|Yİ|YI|DEN|DE|NİN|NIN)\b",RegexOptions.CultureInvariant);
    private static readonly Regex Plain=new(@"\bNashi\b",RegexOptions.CultureInvariant);
    private static readonly Regex Capitals=new(@"\bNASHI\b",RegexOptions.CultureInvariant);

    // Shown text still carries the inherited name. Localization keys must stay as written, so this runs on the result.
    public static string Apply(string text)
    {
        if(AppName==InheritedName||!text.Contains(InheritedName,StringComparison.OrdinalIgnoreCase))return text;
        text=Suffixed.Replace(text,m=>AppName+m.Groups[1].Value+TurkishSuffixes[m.Groups[2].Value]);
        text=SuffixedCapitals.Replace(text,m=>AppName.ToUpperInvariant()+m.Groups[1].Value+TurkishCapitalSuffixes[m.Groups[2].Value]);
        return Capitals.Replace(Plain.Replace(text,AppName),AppName.ToUpperInvariant());
    }
}
