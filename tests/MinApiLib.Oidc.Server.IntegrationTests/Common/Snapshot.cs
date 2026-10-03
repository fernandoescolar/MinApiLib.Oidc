using System.Runtime.CompilerServices;
using System.Text.Encodings.Web;

namespace MinApiLib.Oidc.Server.IntegrationTests.Common;

/// <summary>
/// Minimal snapshot testing: compares a JSON document with a checked-in <c>.verified.json</c> file next to the test.
/// On a mismatch it writes a <c>.received.json</c> file to diff against; accept an intended change by renaming it.
/// </summary>
internal static class Snapshot
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static void Match(JsonNode actual, IEnumerable<string> scrubbedMembers = null, [CallerFilePath] string testFile = "", [CallerMemberName] string name = "")
    {
        Scrub(actual, new HashSet<string>(scrubbedMembers ?? []));
        var text = actual.ToJsonString(Options).ReplaceLineEndings("\n") + "\n";

        var directory = Path.GetDirectoryName(testFile);
        var baseName = Path.Combine(directory, Path.GetFileNameWithoutExtension(testFile) + "." + name);
        var verified = baseName + ".verified.json";
        var received = baseName + ".received.json";

        if (File.Exists(verified) && File.ReadAllText(verified).ReplaceLineEndings("\n") == text)
        {
            File.Delete(received);
            return;
        }

        File.WriteAllText(received, text);
        throw new Xunit.Sdk.XunitException(File.Exists(verified)
            ? $"Snapshot mismatch. Compare {received} with {verified}; if the change is intended, replace the verified file."
            : $"No verified snapshot. Review {received} and rename it to {Path.GetFileName(verified)} to accept it.");
    }

    private static void Scrub(JsonNode node, HashSet<string> members)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj.ToList())
                {
                    if (members.Contains(property.Key))
                    {
                        obj[property.Key] = "{Scrubbed}";
                    }
                    else if (property.Value != null)
                    {
                        Scrub(property.Value, members);
                    }
                }
                break;
            case JsonArray array:
                foreach (var item in array.Where(i => i != null))
                {
                    Scrub(item, members);
                }
                break;
        }
    }
}
