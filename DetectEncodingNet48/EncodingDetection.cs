using System.Text;

namespace DetectEncodingNet48
{
  public class EncodingDetection
  {
    public Encoding Encoding { get; set; }
    public string EncodingName { get; set; }
    public float Confidence { get; set; }
    public bool HasBom { get; set; }
  }
}
