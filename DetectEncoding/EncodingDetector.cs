using System.IO;
using System.Text;
using UtfUnknown;

namespace DetectEncoding
{
  public static class EncodingDetector
  {
    static EncodingDetector()
    {
      // Autorise les encodages Windows historiques
      Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public static EncodingDetection Detect(byte[] data)
    {
      if (data == null || data.Length == 0)
      {
        throw new ArgumentException("Fichier vide.");
      }

      // 1. Détection du BOM
      if (data.Length >= 4)
      {
        // UTF-32 LE (avant UTF-16 LE)
        if (data[0] == 0xFF && data[1] == 0xFE && data[2] == 0x00 && data[3] == 0x00)
        {
          return Result(new UTF32Encoding(false, true), "UTF-32 LE", true);
        }

        // UTF-32 BE
        if (data[0] == 0x00 && data[1] == 0x00 && data[2] == 0xFE && data[3] == 0xFF)
        {
          return Result(new UTF32Encoding(true, true), "UTF-32 BE", true);
        }
      }

      if (data.Length >= 3 && data[0] == 0xEF && data[1] == 0xBB && data[2] == 0xBF)
      {
        return Result(new UTF8Encoding(true), "UTF-8 BOM", true);
      }

      if (data.Length >= 2)
      {
        if (data[0] == 0xFF && data[1] == 0xFE)
        {
          return Result(Encoding.Unicode, "UTF-16 LE", true);
        }

        if (data[0] == 0xFE && data[1] == 0xFF)
        {
          return Result(Encoding.BigEndianUnicode, "UTF-16 BE", true);
        }
      }

      // 2. Détection statistique
      var detection = CharsetDetector.DetectFromBytes(data);
      var best = detection.Detected;

      Encoding encoding = best.Encoding;

      if (encoding == null)
      {
        try
        {
          encoding = Encoding.GetEncoding(best.EncodingName);
        }
        catch
        {
          // Fallback Windows occidental
          encoding = Encoding.GetEncoding(1252);
        }
      }

      return new EncodingDetection
      {
        Encoding = encoding,
        EncodingName = best.EncodingName,
        Confidence = best.Confidence,
        HasBom = false
      };
    }

    private static EncodingDetection Result(Encoding encoding, string name, bool hasBom)
    {
      return new EncodingDetection
      {
        Encoding = encoding,
        EncodingName = name,
        Confidence = 1.0f,
        HasBom = hasBom
      };
    }

    public static EncodingDetection DetectFile(string path)
    {
      byte[] data = File.ReadAllBytes(path);
      return Detect(data);
    }
  }
}
