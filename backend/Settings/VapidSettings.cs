namespace ChatApp.Settings;

public class VapidSettings
{
    public string Subject { get; set; } = "mailto:chatapp@example.com";
    public string PublicKey { get; set; } = string.Empty;
    public string PrivateKey { get; set; } = string.Empty;
}
