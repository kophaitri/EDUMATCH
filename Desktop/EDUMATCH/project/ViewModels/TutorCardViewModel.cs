using System;

namespace EduMatch.ViewModels;

public class TutorCardViewModel
{
    public string Name { get; set; } = string.Empty;
    public string? Subject { get; set; }
    public string? Avatar { get; set; }
    public string? Desc { get; set; }
    public string? Rating { get; set; }
    public string? Price { get; set; }
    public bool Verified { get; set; }
}
