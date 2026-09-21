namespace Gitbers.Models
{
    public class ViberWebhookRequest
    {
        public string Event { get; set; } = string.Empty;

        public ViberUser? User { get; set; }

        public ViberMessage? Message { get; set; }

        public string? MessageToken { get; set; }

        public long Timestamp { get; set; }

        public string? ChatHostname { get; set; }
    }

    public class ViberUser
    {
        public string Id { get; set; } = string.Empty;

        public string? Name { get; set; }

        public string? Avatar { get; set; }

        public string? Language { get; set; }

        public string? Country { get; set; }

        public int? ApiVersion { get; set; }
    }

    public class ViberMessage
    {
        public string Type { get; set; } = string.Empty;

        public string? Text { get; set; }

        public string? Token { get; set; }
    }
}