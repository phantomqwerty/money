using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SEBClone.Models
{
    public class Question
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("text")]
        public string Text { get; set; } = string.Empty;

        [JsonPropertyName("options")]
        public List<string> Options { get; set; } = new();

        [JsonPropertyName("answer")]
        public string Answer { get; set; } = string.Empty;
    }

    public class ExamData
    {
        [JsonPropertyName("examTitle")]
        public string ExamTitle { get; set; } = string.Empty;

        [JsonPropertyName("duration")]
        public int Duration { get; set; }

        [JsonPropertyName("questions")]
        public List<Question> Questions { get; set; } = new();
    }

    /// <summary>
    /// Holds the result of a single exam question for the results review panel.
    /// </summary>
    public class QuestionResult
    {
        public int    QuestionNumber  { get; set; }
        public string QuestionText    { get; set; } = string.Empty;
        public string SelectedAnswer  { get; set; } = string.Empty;
        public string CorrectAnswer   { get; set; } = string.Empty;
        public bool   IsCorrect       { get; set; }
    }
}
