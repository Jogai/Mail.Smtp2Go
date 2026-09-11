using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Scott.Mail.Smtp2Go;

/// <summary>
/// An attachment or inline image for <c>EmailSendRequest</c>. Only <see cref="Filename"/> is required by the API; the content comes from
/// either <see cref="Fileblob"/> (Base64) or <see cref="Url"/> (fetched by SMTP2GO and cached for 24 hours). The factories read files and streams
/// asynchronously at construction time, never during serialisation, and guess <see cref="Mimetype"/> from the file extension when not given.
/// </summary>
public sealed record Attachment
{
    private const int CopyBufferSize = 81920;

    /// <summary>The file name shown to the recipient; for inlines also the <c>cid:</c> reference used in the HTML body.</summary>
    [JsonPropertyName("filename")]
    public required string Filename { get; init; }

    /// <summary>The content, Base64-encoded. Required when <see cref="Url"/> is not set.</summary>
    [JsonPropertyName("fileblob")]
    public string? Fileblob { get; init; }

    /// <summary>The MIME type, for example <c>application/pdf</c>. Optional; guessed from the extension by the factories.</summary>
    [JsonPropertyName("mimetype")]
    public string? Mimetype { get; init; }

    /// <summary>A URL SMTP2GO fetches the content from. Required when <see cref="Fileblob"/> is not set.</summary>
    [JsonPropertyName("url")]
    [SuppressMessage("Design", "CA1056:URI-like properties should not be strings", Justification = "Mirrors the documented wire field; FromUrl accepts a System.Uri.")]
    public string? Url { get; init; }

    /// <summary>Creates an attachment from content that is already Base64-encoded.</summary>
    /// <param name="filename">The file name.</param>
    /// <param name="fileblob">The Base64 content.</param>
    /// <param name="mimetype">The MIME type, or <see langword="null"/> to guess it from the extension of <paramref name="filename"/>.</param>
    public static Attachment FromBase64(string filename, string fileblob, string? mimetype = null)
    {
        Argument.ThrowIfNullOrWhiteSpace(filename);
        Argument.ThrowIfNull(fileblob);
        return new Attachment { Filename = filename, Fileblob = fileblob, Mimetype = mimetype ?? MimeTypeMap.Guess(filename) };
    }

    /// <summary>Creates an attachment from raw bytes, Base64-encoding them.</summary>
    /// <param name="filename">The file name.</param>
    /// <param name="bytes">The content.</param>
    /// <param name="mimetype">The MIME type, or <see langword="null"/> to guess it from the extension of <paramref name="filename"/>.</param>
    public static Attachment FromBytes(string filename, byte[] bytes, string? mimetype = null)
    {
        Argument.ThrowIfNullOrWhiteSpace(filename);
        Argument.ThrowIfNull(bytes);
        return new Attachment { Filename = filename, Fileblob = Convert.ToBase64String(bytes), Mimetype = mimetype ?? MimeTypeMap.Guess(filename) };
    }

    /// <summary>Creates an attachment SMTP2GO fetches from <paramref name="url"/> at send time.</summary>
    /// <param name="filename">The file name.</param>
    /// <param name="url">An absolute URL reachable by SMTP2GO.</param>
    /// <param name="mimetype">The MIME type, or <see langword="null"/> to guess it from the extension of <paramref name="filename"/>.</param>
    public static Attachment FromUrl(string filename, Uri url, string? mimetype = null)
    {
        Argument.ThrowIfNullOrWhiteSpace(filename);
        Argument.ThrowIfNull(url);
        if (!url.IsAbsoluteUri)
        {
            throw new ArgumentException("The attachment URL must be absolute.", nameof(url));
        }

        return new Attachment { Filename = filename, Url = url.AbsoluteUri, Mimetype = mimetype ?? MimeTypeMap.Guess(filename) };
    }

    /// <summary>Reads a file asynchronously and creates an attachment named after it.</summary>
    /// <param name="path">The file to read. Its name (without directory) becomes <see cref="Filename"/>.</param>
    /// <param name="mimetype">The MIME type, or <see langword="null"/> to guess it from the file extension.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    public static async Task<Attachment> FromFileAsync(string path, string? mimetype = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNullOrWhiteSpace(path);
        string filename = Path.GetFileName(path);
        using FileStream stream = new(path, FileMode.Open, FileAccess.Read, FileShare.Read, CopyBufferSize, useAsync: true);
        return await FromStreamAsync(filename, stream, mimetype, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Reads <paramref name="stream"/> to its end asynchronously and creates an attachment. The stream is not disposed.</summary>
    /// <param name="filename">The file name.</param>
    /// <param name="stream">The content; read from its current position.</param>
    /// <param name="mimetype">The MIME type, or <see langword="null"/> to guess it from the extension of <paramref name="filename"/>.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    public static async Task<Attachment> FromStreamAsync(string filename, Stream stream, string? mimetype = null, CancellationToken cancellationToken = default)
    {
        Argument.ThrowIfNullOrWhiteSpace(filename);
        Argument.ThrowIfNull(stream);
        using MemoryStream buffer = new();
        await stream.CopyToAsync(buffer, CopyBufferSize, cancellationToken).ConfigureAwait(false);
        string fileblob = Convert.ToBase64String(buffer.GetBuffer(), 0, checked((int)buffer.Length));
        return new Attachment { Filename = filename, Fileblob = fileblob, Mimetype = mimetype ?? MimeTypeMap.Guess(filename) };
    }
}
