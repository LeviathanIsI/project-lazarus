namespace Lazarus.Shared.Images;

public enum ImageGenEventKind { Progress, Info, Completed, Error }

public sealed record ImageGenEvent(
    ImageGenEventKind Kind,
    double? Progress = null,
    string? Message = null,
    byte[]? ImagePng = null
)
{
    public static ImageGenEvent Info(string m) => new(ImageGenEventKind.Info, null, m);
    public static ImageGenEvent Error(string m) => new(ImageGenEventKind.Error, null, m);
    public static ImageGenEvent Done(byte[] png) => new(ImageGenEventKind.Completed, 1.0, "Done", png);
    public static ImageGenEvent Tick(double? p, string? m = null) => new(ImageGenEventKind.Progress, p, m);
}

