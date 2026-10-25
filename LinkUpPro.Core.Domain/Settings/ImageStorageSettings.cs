namespace LinkUpPro.Core.Domain.Settings
{
    public class ImageStorageSettings
    {
        // Physical folder served as static files (the Web project's wwwroot); images live under its "Images" folder.
        public string RootPath { get; set; } = string.Empty;
    }
}
