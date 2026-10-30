using System.Net.Http;
using System.Runtime.InteropServices;
namespace Nashi.Windows;

// Raw exception text (SSL stack messages, Windows imaging codes, English-only library errors) is not something a person
// can act on. Failures we know about get a short instruction; every other exception keeps its own message.
internal static class FriendlyErrors
{
    private const int ComponentNotFound=unchecked((int)0x88982F50);
    internal static string Message(Exception error)
    {
        for(var inner=error;inner is not null;inner=inner.InnerException)
        {
            if(inner is HttpRequestException or System.Net.Sockets.SocketException or System.Security.Authentication.AuthenticationException)return "The connection failed. Check your internet connection and try again.";
            if(inner is UnauthorizedAccessException)return "Nashi cannot write to that folder. Choose another folder in Settings.";
            if(inner is PathTooLongException)return "The file path is too long. Move the file closer to the top of the drive and try again.";
            if(inner is FileFormatException||inner.HResult==ComponentNotFound)return "This image could not be read. It may be damaged or in an unsupported format.";
        }
        return error.Message;
    }
}
