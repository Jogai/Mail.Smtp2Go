using System.Text;

namespace Scott.Mail.Smtp2Go.Tests.Unit.Email;

public class AttachmentTests
{
    private static readonly byte[] s_content = Encoding.UTF8.GetBytes("hello attachment");
    private static readonly string s_contentBase64 = Convert.ToBase64String(s_content);

    [Fact]
    public void FromBase64_keeps_the_blob_and_guesses_the_mimetype()
    {
        Attachment attachment = Attachment.FromBase64("report.pdf", s_contentBase64);

        attachment.Should().Be(new Attachment { Filename = "report.pdf", Fileblob = s_contentBase64, Mimetype = "application/pdf" });
        attachment.Url.Should().BeNull();
    }

    [Fact]
    public void FromBytes_encodes_the_content()
    {
        Attachment attachment = Attachment.FromBytes("notes.txt", s_content);

        attachment.Fileblob.Should().Be(s_contentBase64);
        attachment.Mimetype.Should().Be("text/plain");
        attachment.Filename.Should().Be("notes.txt");
    }

    [Fact]
    public void FromUrl_sets_the_url_and_leaves_the_blob_empty()
    {
        Attachment attachment = Attachment.FromUrl("logo.png", new Uri("https://example.com/assets/logo.png"));

        attachment.Url.Should().Be("https://example.com/assets/logo.png");
        attachment.Fileblob.Should().BeNull();
        attachment.Mimetype.Should().Be("image/png");
    }

    [Fact]
    public void FromUrl_rejects_relative_urls()
    {
        Func<Attachment> act = () => Attachment.FromUrl("logo.png", new Uri("assets/logo.png", UriKind.Relative));

        act.Should().Throw<ArgumentException>().WithParameterName("url");
    }

    [Fact]
    public async Task FromFileAsync_reads_the_file_and_names_the_attachment_after_it()
    {
        string directory = Path.Combine(Path.GetTempPath(), "smtp2go-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "invoice.PDF");
        try
        {
            await File.WriteAllBytesAsync(path, s_content, TestContext.Current.CancellationToken);

            Attachment attachment = await Attachment.FromFileAsync(path, cancellationToken: TestContext.Current.CancellationToken);

            attachment.Filename.Should().Be("invoice.PDF");
            attachment.Fileblob.Should().Be(s_contentBase64);
            attachment.Mimetype.Should().Be("application/pdf");
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public async Task FromFileAsync_missing_file_throws_the_io_exception()
    {
        Func<Task> act = () => Attachment.FromFileAsync(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"), "missing.txt"), cancellationToken: TestContext.Current.CancellationToken);

        await act.Should().ThrowAsync<IOException>();
    }

    [Fact]
    public async Task FromStreamAsync_reads_from_the_current_position_and_does_not_dispose()
    {
        using MemoryStream stream = new(Encoding.UTF8.GetBytes("skip-" + "hello attachment"));
        stream.Position = 5;

        Attachment attachment = await Attachment.FromStreamAsync("data.bin", stream, cancellationToken: TestContext.Current.CancellationToken);

        attachment.Fileblob.Should().Be(s_contentBase64);
        attachment.Mimetype.Should().BeNull();
        stream.CanRead.Should().BeTrue();
    }

    [Fact]
    public async Task Explicit_mimetype_overrides_the_guess()
    {
        Attachment.FromBase64("a.pdf", s_contentBase64, "application/octet-stream").Mimetype.Should().Be("application/octet-stream");
        Attachment.FromBytes("a.pdf", s_content, "x/y").Mimetype.Should().Be("x/y");
        Attachment.FromUrl("a.pdf", new Uri("https://example.com/a"), "x/y").Mimetype.Should().Be("x/y");
        using MemoryStream stream = new(s_content);
        (await Attachment.FromStreamAsync("a.pdf", stream, "x/y", TestContext.Current.CancellationToken)).Mimetype.Should().Be("x/y");
    }

    [Theory]
    [InlineData("photo.JPG", "image/jpeg")]
    [InlineData("archive.tar.gz", "application/gzip")]
    [InlineData("sheet.xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("calendar.ics", "text/calendar")]
    [InlineData("noextension", null)]
    [InlineData("trailingdot.", null)]
    [InlineData("weird.xyz123", null)]
    public void Mimetype_is_guessed_from_the_extension(string filename, string? expected)
    {
        MimeTypeMap.Guess(filename).Should().Be(expected);
    }

    [Fact]
    public void Factories_reject_blank_filenames_and_null_content()
    {
        Func<Attachment> blank = () => Attachment.FromBase64(" ", s_contentBase64);
        Func<Attachment> nullBlob = () => Attachment.FromBase64("a.txt", null!);
        Func<Attachment> nullBytes = () => Attachment.FromBytes("a.txt", null!);
        Func<Attachment> nullUrl = () => Attachment.FromUrl("a.txt", null!);
        Func<Task<Attachment>> nullStream = () => Attachment.FromStreamAsync("a.txt", null!, cancellationToken: TestContext.Current.CancellationToken);

        blank.Should().Throw<ArgumentException>();
        nullBlob.Should().Throw<ArgumentNullException>();
        nullBytes.Should().Throw<ArgumentNullException>();
        nullUrl.Should().Throw<ArgumentNullException>();
        nullStream.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public void CustomHeader_is_a_positional_record()
    {
        CustomHeader header = new("X-Campaign", "spring");

        header.Header.Should().Be("X-Campaign");
        header.Value.Should().Be("spring");
        header.Should().Be(new CustomHeader("X-Campaign", "spring"));
    }
}
