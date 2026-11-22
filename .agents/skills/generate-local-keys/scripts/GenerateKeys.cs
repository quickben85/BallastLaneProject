using System.Runtime.Versioning;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;

if (!File.Exists("compose.yaml"))
{
    Console.Error.WriteLine("Run from the Task Manager repository root.");
    return 1;
}

try
{
    var directory = new DirectoryInfo(".local");
    ProtectKeyDirectory(directory);
    var privatePath = Path.Combine(directory.FullName, "accounts-private.pem");
    var publicPath = Path.Combine(directory.FullName, "accounts-public.pem");
    RequireRegularKeyPath(privatePath);
    RequireRegularKeyPath(publicPath);

    using var key = RSA.Create(2048);
    if (File.Exists(privatePath))
    {
        key.ImportFromPem(File.ReadAllText(privatePath));
        _ = key.ExportParameters(includePrivateParameters: true);
        if (key.KeySize < 2048)
        {
            throw new InvalidOperationException("The existing private key must be at least 2048 bits.");
        }
    }
    else
    {
        if (File.Exists(publicPath))
        {
            throw new InvalidOperationException("The public key exists without its private key. Restore the original private key.");
        }

        WriteNewKey(privatePath, key.ExportPkcs8PrivateKeyPem());
    }

    if (File.Exists(publicPath))
    {
        using var publicKey = RSA.Create();
        var pem = File.ReadAllText(publicPath).Trim();
        if (!PemEncoding.TryFind(pem, out var fields)
            || !pem.AsSpan()[fields.Label].SequenceEqual("PUBLIC KEY")
            || pem[fields.Location].Length != pem.Length)
        {
            throw new InvalidOperationException("The existing public key must be a PUBLIC KEY PEM.");
        }

        publicKey.ImportFromPem(pem);
        if (!publicKey.ExportSubjectPublicKeyInfo().SequenceEqual(key.ExportSubjectPublicKeyInfo()))
        {
            throw new InvalidOperationException("The existing private and public keys do not match.");
        }
    }
    else
    {
        WriteNewKey(publicPath, key.ExportSubjectPublicKeyInfoPem());
    }

    if (!OperatingSystem.IsWindows())
    {
        const UnixFileMode containerReadable = UnixFileMode.UserRead | UnixFileMode.UserWrite
            | UnixFileMode.GroupRead | UnixFileMode.OtherRead;
        // The user-only parent directory protects the files on the host. The APIs mount individual files.
        File.SetUnixFileMode(privatePath, containerReadable);
        File.SetUnixFileMode(publicPath, containerReadable);
    }

    Console.WriteLine("Local signing key pair ready in .local/.");
    return 0;
}
catch (Exception error) when (error is IOException or UnauthorizedAccessException or CryptographicException
    or ArgumentException or InvalidOperationException)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}

static void RequireRegularKeyPath(string path)
{
    if (Directory.Exists(path) || new FileInfo(path).LinkTarget is not null)
    {
        throw new InvalidOperationException($"Use a regular file for {Path.GetFileName(path)}.");
    }
}

static void WriteNewKey(string path, string pem)
{
    var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
    try
    {
        File.WriteAllText(temporaryPath, pem + "\n");
        File.Move(temporaryPath, path);
    }
    finally
    {
        File.Delete(temporaryPath);
    }
}

static void ProtectKeyDirectory(DirectoryInfo directory)
{
    if (directory.Exists && (directory.Attributes & FileAttributes.ReparsePoint) != 0)
    {
        throw new InvalidOperationException("Use a regular directory for .local/.");
    }

    if (OperatingSystem.IsWindows())
    {
        ProtectWindowsKeyDirectory(directory);
        return;
    }

    const UnixFileMode ownerOnly = UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute;
    Directory.CreateDirectory(directory.FullName, ownerOnly);
    File.SetUnixFileMode(directory.FullName, ownerOnly);
}

[SupportedOSPlatform("windows")]
static void ProtectWindowsKeyDirectory(DirectoryInfo directory)
{
    using var identity = WindowsIdentity.GetCurrent();
    var user = identity.User ?? throw new InvalidOperationException("Could not determine the current Windows user.");
    var system = new SecurityIdentifier(WellKnownSidType.LocalSystemSid, null);
    var security = new DirectorySecurity();
    security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
    const InheritanceFlags children = InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit;
    security.AddAccessRule(new FileSystemAccessRule(user, FileSystemRights.FullControl,
        children, PropagationFlags.None, AccessControlType.Allow));
    security.AddAccessRule(new FileSystemAccessRule(system, FileSystemRights.ReadAndExecute,
        children, PropagationFlags.None, AccessControlType.Allow));

    if (directory.Exists)
    {
        directory.SetAccessControl(security);
    }
    else
    {
        directory.Create(security);
    }
}
