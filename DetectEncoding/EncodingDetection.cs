using System.Text;

namespace DetectEncoding
{
  public class EncodingDetection
  {
    public Encoding Encoding { get; init; }
    public string EncodingName { get; init; }
    public float Confidence { get; init; }
    public bool HasBom { get; init; }
  }
}
