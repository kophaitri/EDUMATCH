using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace EduMatch.ViewModels;

public class ExamListItemViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int DurationMinutes { get; set; }
    public int PassingScore { get; set; }
    public ExamStatus Status { get; set; }
    public bool IsOpen { get; set; }
    public DateTime? OpenAt { get; set; }
    public DateTime? CloseAt { get; set; }
    public string? ExamFileUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public int TotalQuestions { get; set; }
    public int TotalSubmissions { get; set; }
}

public class CreateExamViewModel
{
    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    [Range(1, 300, ErrorMessage = "Thời gian phải từ 1 đến 300 phút")]
    public int DurationMinutes { get; set; } = 60;

    [Range(0, 10000, ErrorMessage = "Điểm qua môn không hợp lệ")]
    public int PassingScore { get; set; } = 50;

    [Range(0, 10)]
    public int MaxRetakes { get; set; } = 2;

    public IFormFile? ExamFile { get; set; }
    public List<CreateQuestionViewModel> Questions { get; set; } = new();
}

public class CreateQuestionViewModel
{
    public string QuestionText { get; set; } = string.Empty;
    public int Points { get; set; } = 1;
    public int CorrectOptionIndex { get; set; } = 0;
    public List<CreateAnswerOptionViewModel> Options { get; set; } = new()
    {
        new(), new(), new(), new()
    };
}

public class CreateAnswerOptionViewModel
{
    public string OptionText { get; set; } = string.Empty;
}

public class EditExamViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }

    [Range(1, 300)]
    public int DurationMinutes { get; set; }

    [Range(0, 10000)]
    public int PassingScore { get; set; }

    [Range(0, 10)]
    public int MaxRetakes { get; set; }

    public string? ExistingFileUrl { get; set; }
    public IFormFile? ExamFile { get; set; }
    public List<CreateQuestionViewModel> NewQuestions { get; set; } = new();
}

public class UpdateExamQuestionViewModel
{
    public int QuestionId { get; set; }
    public int ExamId { get; set; }

    [Required(ErrorMessage = "Nội dung câu hỏi không được trống")]
    public string QuestionText { get; set; } = string.Empty;
    public string? PassageText { get; set; }

    [Range(1, 100)]
    public int Points { get; set; } = 1;

    [Required]
    public string CorrectOption { get; set; } = "A";

    [Required]
    public string OptionA { get; set; } = string.Empty;
    [Required]
    public string OptionB { get; set; } = string.Empty;
    [Required]
    public string OptionC { get; set; } = string.Empty;
    [Required]
    public string OptionD { get; set; } = string.Empty;
}

public class GradeSubmissionViewModel
{
    public int ExamId { get; set; }
    public int SubmissionId { get; set; }
    public string StudentName { get; set; } = string.Empty;
    public decimal CurrentScore { get; set; }
    public decimal MaxScore { get; set; }
    public string? TutorComment { get; set; }

    [Range(0, 10000)]
    public decimal? OverrideScore { get; set; }
}
