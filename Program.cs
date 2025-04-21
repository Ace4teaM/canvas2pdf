using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.UniversalAccessibility.Drawing;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

internal class Program
{
    internal enum ContentType
    {
        Undefined,
        Text_UTF8,
        Image_PNG,
        Image_JPEG
    }

    private static void Main(string[] args)
    {
        PdfDocument doc;

        if (File.Exists(args[0]))
        {
            doc = PdfReader.Open(args[0]);
        }
        else
        {
            doc = new PdfDocument();
            doc.PageLayout = PdfPageLayout.SinglePage;
        }

        Stream? content = null;
        string outputFile = args[0];
        int pageCount = -1;
        ContentType contentType = ContentType.Undefined;
        XRect xRect = new XRect(0, 0, 0, 0);
        XFont xFont = new XFont("Arial", 20);

        if (args.Contains("-i"))
        {
            content = File.OpenRead(args[Array.IndexOf(args, "-i") + 1]);
        }

        if (args.Contains("-o"))
        {
            outputFile = args[Array.IndexOf(args, "-o") + 1];
        }

        if (args.Contains("-p"))
        {
            pageCount = int.Parse(args[Array.IndexOf(args, "-p") + 1]);
        }

        PdfPage page = pageCount == -1 ? doc.AddPage() : doc.Pages[pageCount];
        if (args.Contains("-l"))
        {
            page.Orientation = Enum.Parse<PdfSharp.PageOrientation>(args[Array.IndexOf(args, "-l") + 1]);
        }

        if (args.Contains("-t"))
        {
            contentType = Enum.Parse<ContentType>(args[Array.IndexOf(args, "-t") + 1]);
        }

        if (args.Contains("-r"))
        {
            try
            {
                xRect = XRect.Parse(args[Array.IndexOf(args, "-r") + 1]);
            }
            catch
            {
                xRect = new XRect();
                var parse = args[Array.IndexOf(args, "-r") + 1].Split(',');

                xRect.X = (parse[0].EndsWith("%")) ? ParsePercentHorizontal(page, parse[0]) : ParseMesure(parse[0]);
                xRect.Y = (parse[1].EndsWith("%")) ? ParsePercentVertical(page, parse[1]) : ParseMesure(parse[1]);
                xRect.Width = (parse[2].EndsWith("%")) ? ParsePercentHorizontal(page, parse[2]) : ParseMesure(parse[2]);
                xRect.Height = (parse[3].EndsWith("%")) ? ParsePercentVertical(page, parse[3]) : ParseMesure(parse[3]);
            }
        }

        if (content != null)
        {
            if (contentType == ContentType.Undefined && IsJPEG(content))
            {
                contentType = ContentType.Image_JPEG;
            }

            if (contentType == ContentType.Undefined && IsPNG(content))
            {
                contentType = ContentType.Image_PNG;
            }

            if (contentType == ContentType.Undefined && IsUTF8(content))
            {
                contentType = ContentType.Text_UTF8;
            }

            try
            {
                switch (contentType)
                {
                    case ContentType.Undefined:
                        Console.WriteLine("Can't detect content type ! Please specify in command, ie: -t image");
                        break;

                    case ContentType.Text_UTF8:
                        var stream = new MemoryStream();
                        content.CopyTo(stream);
                        var text = Encoding.UTF8.GetString(stream.GetBuffer(), 0, (int)stream.Length);

                        using (XGraphics g = XGraphics.FromPdfPage(page))
                        {
                            g.DrawString(text, xFont, XBrushes.Black, xRect, XStringFormats.Center);
                        }
                        break;

                    case ContentType.Image_PNG:
                    case ContentType.Image_JPEG:
                        using (XGraphics g = XGraphics.FromPdfPage(page))
                        {
                            XImage image = XImage.FromStream(content);
                            g.DrawImage(image, xRect);
                        }
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        if (File.Exists(outputFile))
            File.Delete(outputFile);

        doc.Save(outputFile);
    }

    internal static double ParsePercentVertical(PdfPage page, string value)
    {
        if (value.EndsWith("%"))
        {
            return (page.Height.Value / 100.0) * double.Parse(value.Substring(0, value.Length - 1));
        }

        return (page.Height.Value / 100.0) * double.Parse(value);
    }
    internal static double ParsePercentHorizontal(PdfPage page, string value)
    {
        if (value.EndsWith("%"))
        {
            return (page.Width.Value / 100.0) * double.Parse(value.Substring(0, value.Length - 1));
        }

        return (page.Width.Value / 100.0) * double.Parse(value);
    }
    internal static double ParseMesure(string value)
    {
        if (value.EndsWith("cm"))
        {
            return XUnit.FromCentimeter(double.Parse(value.Substring(0, value.Length - 1))).Value;
        }
        else if (value.EndsWith("mm"))
        {
            return XUnit.FromMillimeter(double.Parse(value.Substring(0, value.Length - 1))).Value;
        }
        else
        {
            return double.Parse(value);
        }
    }
    internal static bool IsUTF8(Stream stream)
    {
        var expected_header = new byte[] { 0xEF, 0xBB, 0xBF };
        var header = new byte[expected_header.Length];
        var count = stream.Read(header, 0, expected_header.Length);
        stream.Seek(0, SeekOrigin.Begin);

        return (count == expected_header.Length && header.SequenceEqual(expected_header));
    }
    internal static bool IsPNG(Stream stream)
    {
        var expected_header = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        var header = new byte[expected_header.Length];
        var count = stream.Read(header, 0, expected_header.Length);
        stream.Seek(0, SeekOrigin.Begin);

        return (count == expected_header.Length && header.SequenceEqual(expected_header));
    }
    internal static bool IsJPEG(Stream stream)
    {
        var expected_header = new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 };
        var header = new byte[expected_header.Length];
        var count = stream.Read(header, 0, expected_header.Length);
        stream.Seek(0, SeekOrigin.Begin);

        return (count == expected_header.Length && header.SequenceEqual(expected_header));
    }
}