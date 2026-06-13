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
}
