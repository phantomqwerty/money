using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SEBClone.Models
{
    /// <summary>Represents a single student entry in Data/users.json.</summary>
    internal sealed class User
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("secretKey")]
        public string SecretKey { get; set; } = string.Empty;
    }

    /// <summary>Root object of Data/users.json.</summary>
    internal sealed class UserList
    {
        [JsonPropertyName("students")]
        public List<User> Students { get; set; } = new();
    }
}
