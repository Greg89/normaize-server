using System.ComponentModel.DataAnnotations;

namespace Normaize.Core.Models;

public class FileUploadRequest
{
    [Required]
    public string FileName { get; set; } = string.Empty;
    
    [Required]
    public string ContentType { get; set; } = string.Empty;
    
    public long FileSize { get; set; }
    
    public Stream FileStream { get; set; } = Stream.Null;
} 