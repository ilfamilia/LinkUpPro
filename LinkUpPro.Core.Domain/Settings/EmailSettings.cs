namespace LinkUpPro.Core.Domain.Settings
{
    public class EmailSettings
    {
        public required string EmailFrom { get; set; }
        public required string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public required string SmtpUser { get; set; }
        public required string SmtpPass { get; set; }
        public required string DisplayName { get; set; }

        // Optional local folder: when set, emails are saved as .eml files instead of sent through SMTP.
        public string? PickupDirectory { get; set; }
    }    
}
