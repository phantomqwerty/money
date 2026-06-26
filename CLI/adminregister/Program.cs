// ============================================================
//  AdminRegister — registers username/secretKey pairs for the
//  SEBClone Ctrl+Alt+Shift+G bypass feature.
//
//  Data resolution:
//    - Reads/writes Data/override_users.json next to this exe
//      (linked from the solution root by AdminRegister.csproj).
//    - For integration, ensure both this tool and the sebbypass
//      tool share the same Data/ directory (copy or symlink).
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AdminRegister
{
    // ── Data model ────────────────────────────────────────────────────────────

    /// <summary>Represents one bypass-eligible operator in override_users.json.</summary>
    internal sealed class OverrideUser
    {
        [JsonPropertyName("username")]
        public string Username { get; set; } = string.Empty;

        [JsonPropertyName("secretKey")]
        public string SecretKey { get; set; } = string.Empty;
    }

    // ── Entry point ───────────────────────────────────────────────────────────

    internal static class Program
    {
        private static readonly string DataDir =
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");

        private static readonly string OverrideUsersPath =
            Path.Combine(DataDir, "override_users.json");

        private static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            PrintHeader();

            // ── Collect credentials ───────────────────────────────────────────
            Console.Write("Username: ");
            string username = Console.ReadLine()?.Trim() ?? string.Empty;

            Console.Write("Secret Key: ");
            string secretKey = Console.ReadLine()?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(secretKey))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("\n   Username and Secret Key must not be empty.");
                Console.ResetColor();
                ExitPrompt();
                return;
            }

            // ── Load existing entries ─────────────────────────────────────────
            List<OverrideUser> users = LoadUsers();

            // ── Check for duplicate ───────────────────────────────────────────
            bool alreadyExists = users.Exists(u =>
                string.Equals(u.Username, username, StringComparison.Ordinal));

            if (alreadyExists)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n   Already registered: \"{username}\" — no changes made.");
                Console.ResetColor();
                ExitPrompt();
                return;
            }

            // ── Append and save ───────────────────────────────────────────────
            users.Add(new OverrideUser { Username = username, SecretKey = secretKey });
            SaveUsers(users);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n   \u2713 Registered \"{username}\" successfully.");
            Console.ResetColor();

            ExitPrompt();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static void PrintHeader()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine();
            Console.WriteLine("=== SEB Admin Register ===");
            Console.ResetColor();
            Console.WriteLine();
        }

        /// <summary>
        /// Reads <c>Data/override_users.json</c>. Returns an empty list when the
        /// file is absent, empty, or contains malformed JSON (with a warning).
        /// </summary>
        private static List<OverrideUser> LoadUsers()
        {
            if (!File.Exists(OverrideUsersPath))
                return new List<OverrideUser>();

            string json = File.ReadAllText(OverrideUsersPath).Trim();
            if (string.IsNullOrEmpty(json) || json == "[]")
                return new List<OverrideUser>();

            try
            {
                return JsonSerializer.Deserialize<List<OverrideUser>>(json)
                       ?? new List<OverrideUser>();
            }
            catch (JsonException ex)
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"   Warning: could not parse override_users.json — starting fresh. ({ex.Message})");
                Console.ResetColor();
                return new List<OverrideUser>();
            }
        }

        /// <summary>
        /// Serialises <paramref name="users"/> back to <c>Data/override_users.json</c>,
        /// creating the Data directory if it does not exist.
        /// </summary>
        private static void SaveUsers(List<OverrideUser> users)
        {
            Directory.CreateDirectory(DataDir);

            string json = JsonSerializer.Serialize(users, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            File.WriteAllText(OverrideUsersPath, json);
        }

        private static void ExitPrompt()
        {
            Console.WriteLine();
            Console.Write("Press any key to exit...");
            Console.ReadKey(intercept: true);
        }
    }
}
