using System.ComponentModel.DataAnnotations;

namespace Ape.Models;

public class EmailLog
{
    public int Id { get; set; }

    [Required]
    public DateTime Timestamp { get; set; }

    [Required]
    [MaxLength(255)]
    public string ToEmail { get; set; } = string.Empty;

    [MaxLength(255)]
    public string? FromEmail { get; set; }

    [Required]
    [MaxLength(500)]
    public string Subject { get; set; } = string.Empty;

    public bool Success { get; set; }

    [MaxLength(1000)]
    public string? Details { get; set; }

    [MaxLength(50)]
    public string EmailServer { get; set; } = "Unknown";

    [MaxLength(100)]
    public string? MessageId { get; set; }

    public int? RetryCount { get; set; } = 0;

    [MaxLength(100)]
    public string? SentBy { get; set; }

    [MaxLength(50)]
    public string? EmailType { get; set; }

    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}
