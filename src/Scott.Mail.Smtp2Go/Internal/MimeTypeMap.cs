namespace Scott.Mail.Smtp2Go;

/// <summary>Guesses a MIME type from a file extension for the common attachment types, without depending on System.Web or a platform registry.</summary>
internal static class MimeTypeMap
{
    private static readonly Dictionary<string, string> s_byExtension = new(StringComparer.OrdinalIgnoreCase)
    {
        // Documents
        ["pdf"] = "application/pdf",
        ["doc"] = "application/msword",
        ["docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ["xls"] = "application/vnd.ms-excel",
        ["xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ["ppt"] = "application/vnd.ms-powerpoint",
        ["pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        ["odt"] = "application/vnd.oasis.opendocument.text",
        ["ods"] = "application/vnd.oasis.opendocument.spreadsheet",
        ["odp"] = "application/vnd.oasis.opendocument.presentation",
        ["rtf"] = "application/rtf",
        ["epub"] = "application/epub+zip",

        // Text
        ["txt"] = "text/plain",
        ["md"] = "text/markdown",
        ["csv"] = "text/csv",
        ["html"] = "text/html",
        ["htm"] = "text/html",
        ["css"] = "text/css",
        ["js"] = "text/javascript",
        ["json"] = "application/json",
        ["xml"] = "application/xml",
        ["ics"] = "text/calendar",
        ["vcf"] = "text/vcard",
        ["eml"] = "message/rfc822",

        // Images
        ["png"] = "image/png",
        ["jpg"] = "image/jpeg",
        ["jpeg"] = "image/jpeg",
        ["gif"] = "image/gif",
        ["webp"] = "image/webp",
        ["svg"] = "image/svg+xml",
        ["bmp"] = "image/bmp",
        ["ico"] = "image/x-icon",
        ["tif"] = "image/tiff",
        ["tiff"] = "image/tiff",
        ["heic"] = "image/heic",

        // Audio and video
        ["mp3"] = "audio/mpeg",
        ["wav"] = "audio/wav",
        ["ogg"] = "audio/ogg",
        ["m4a"] = "audio/mp4",
        ["mp4"] = "video/mp4",
        ["mpeg"] = "video/mpeg",
        ["webm"] = "video/webm",
        ["avi"] = "video/x-msvideo",
        ["mov"] = "video/quicktime",

        // Archives
        ["zip"] = "application/zip",
        ["gz"] = "application/gzip",
        ["tar"] = "application/x-tar",
        ["7z"] = "application/x-7z-compressed",
        ["rar"] = "application/vnd.rar",

        // Fonts
        ["ttf"] = "font/ttf",
        ["otf"] = "font/otf",
        ["woff"] = "font/woff",
        ["woff2"] = "font/woff2",
    };

    /// <summary>Returns the MIME type for the extension of <paramref name="filename"/>, or <see langword="null"/> when it is unknown or there is no extension.</summary>
    public static string? Guess(string filename)
    {
        string extension = Path.GetExtension(filename);
        if (extension.Length < 2)
        {
            return null;
        }

        return s_byExtension.TryGetValue(extension.Substring(1), out string? mimetype) ? mimetype : null;
    }
}
