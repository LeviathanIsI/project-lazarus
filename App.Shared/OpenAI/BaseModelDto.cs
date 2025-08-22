namespace Lazarus.Shared.OpenAI;

public class BaseModelDto
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string FileName { get; set; } = "";
    public string Size { get; set; } = "";
    public string Format { get; set; } = "";
    public string Architecture { get; set; } = "";
    public int ContextLength { get; set; }
    public string Quantization { get; set; } = "";
    public bool IsActive { get; set; }
}
