using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace ArchivumWpf.Models;

[Table("activity_log")]
public class ActivityLog
{
    [Key]
    public int Id { get; set; }
    
    [Required]
    public string SerialNumber { get; set; } = string.Empty;
    [Required]
    public string RrNumber { get; set; } = string.Empty;
    [Required]
    public string ActionType { get; set; } = string.Empty;
    
    public DateTime Timestamp { get; set; } = DateTime.Now;
    
}