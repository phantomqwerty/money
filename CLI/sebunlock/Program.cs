// ============================================================
//  SebUnlock — CLI unlock tool for the SEBClone kiosk app
//  Expected working directory / Data resolution:
//    - On build, Data/users.json is copied next to this exe
//      (see SebUnlock.csproj ItemGroup).
//    - The unlock flag is written to Data/unlock.flag in the
//      same directory, which the main WinForms app must also
//      check (resolved from its own BaseDirectory).
//    - For integration, copy / symlink Data/ to a shared path
//      that both executables can read.
// ============================================================

using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SebUnlock
{
    // ── Deserialization models ────────────────────────────────────────────────

    /// <summary>Represents a single student entry in users.json.</summary>
    internal sealed class User
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("secretKey")]
        public string SecretKey { get; set; } = string.Empty;
    }

    /// <summary>Root object of users.json.</summary>
    internal sealed class UserList
    {
        [JsonPropertyName("students")]
        public List<User> Students { get; set; } = new();
    }

    // ── Entry point ───────────────────────────────────────────────────────────

    internal static class Program
    {
        // Paths resolved relative to the directory that contains this exe.
        private static readonly string BaseDir =
            AppDomain.CurrentDomain.BaseDirectory;

        private static readonly string UsersJsonPath =
            Path.Combine(BaseDir, "Data", "users.json");

        private static readonly string UnlockFlagPath =
            Path.Combine(BaseDir, "Data", "unlock.flag");

        private static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            PrintHeader();

            // ── Read credentials ──────────────────────────────────────────────
            Console.Write("Enter username: ");
            string username = Console.ReadLine()?.Trim() ?? string.Empty;

            Console.Write("Enter secret key: ");
            string secretKey = Console.ReadLine()?.Trim() ?? string.Empty;

            // ── Load and validate ─────────────────────────────────────────────
            bool granted = TryValidate(username, secretKey, out string? error);

            Console.WriteLine();

            if (granted)
            {
                WriteUnlockFlag(username);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("   \u2713 Unlock granted. You may now minimize SEB.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("   \u2717 Invalid username or secret key.");
                if (error is not null)
                {
                    Console.ForegroundColor = ConsoleColor.DarkYellow;
                    Console.WriteLine($"   (detail: {error})");
                }
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.Write("Press any key to exit...");
            Console.ReadKey(intercept: true);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine("=== SEB Unlock Tool ===");
            Console.ResetColor();
            Console.WriteLine();
        }

        /// <summary>
        /// Loads users.json and checks whether <paramref name="username"/> and
        /// <paramref name="secretKey"/> match any entry (case-sensitive).
        /// </summary>
        /// <param name="username">Username entered by the operator.</param>
        /// <param name="secretKey">Secret key entered by the operator.</param>
        /// <param name="error">
        /// Populated with a diagnostic message when validation cannot be performed
        /// (e.g. file missing or malformed JSON); <c>null</c> on success or a clean mismatch.
        /// </param>
        /// <returns><c>true</c> if credentials match; <c>false</c> otherwise.</returns>
        private static bool TryValidate(string username, string secretKey, out string? error)
        {
            error = null;

            if (!File.Exists(UsersJsonPath))
            {
                error = $"users.json not found at: {UsersJsonPath}";
                return false;
            }

            UserList? userList;
            try
            {
                string json = File.ReadAllText(UsersJsonPath);
                userList = JsonSerializer.Deserialize<UserList>(json);
            }
            catch (JsonException ex)
            {
                error = $"Failed to parse users.json — {ex.Message}";
                return false;
            }

            if (userList is null || userList.Students.Count == 0)
            {
                error = "users.json contains no student entries.";
                return false;
            }

            return userList.Students.Exists(u =>
                string.Equals(u.Username, username, StringComparison.Ordinal) &&
                string.Equals(u.SecretKey, secretKey, StringComparison.Ordinal));
        }

        /// <summary>
        /// Creates (or overwrites) <c>Data/unlock.flag</c> with the username and
        /// a UTC timestamp.  The WinForms app polls for this file via
        /// <c>LockdownManager.IsUnlocked()</c>.
        /// </summary>
        private static void WriteUnlockFlag(string username)
        {
            // Ensure the Data directory exists next to the exe.
            string dataDir = Path.GetDirectoryName(UnlockFlagPath)!;
            Directory.CreateDirectory(dataDir);

            File.WriteAllText(
                UnlockFlagPath,
                $"unlocked_by={username}{Environment.NewLine}" +
                $"timestamp={DateTime.UtcNow:O}{Environment.NewLine}");
        }
    }
}
