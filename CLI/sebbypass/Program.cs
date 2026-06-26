// ============================================================
//  SebBypass — validates an override username/secretKey pair
//  and writes Data/bypass.flag on success, enabling the
//  Ctrl+Alt+Shift+G bypass inside the SEBClone kiosk app.
//
//  Data resolution:
//    - Reads  Data/override_users.json next to this exe
//      (linked from the solution root by SebBypass.csproj).
//    - Writes Data/bypass.flag in the same directory.
//    - For integration, ensure both this tool and the main
//      WinForms app share the same Data/ directory (copy or
//      symlink) so LockdownManager.IsBypassed() sees the flag.
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace SebBypass
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

        private static readonly string BypassFlagPath =
            Path.Combine(DataDir, "bypass.flag");

        private static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            PrintHeader();

            // ── Collect credentials ───────────────────────────────────────────
            Console.Write("Username: ");
            string username = Console.ReadLine()?.Trim() ?? string.Empty;

            Console.Write("Secret Key: ");
            string secretKey = Console.ReadLine()?.Trim() ?? string.Empty;

            Console.WriteLine();

            // ── Load and match ────────────────────────────────────────────────
            bool matched = TryMatch(username, secretKey, out string? error);

            if (matched)
            {
                WriteBypassFlag(username);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("   \u2713 Now you can minimize the system.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("   No match found.");
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
            Console.WriteLine("=== SEB Bypass Tool ===");
            Console.ResetColor();
            Console.WriteLine();
        }

        /// <summary>
        /// Loads <c>Data/override_users.json</c> and checks for an exact
        /// (case-sensitive) username + secretKey match.
        /// </summary>
        /// <param name="username">Username entered by the operator.</param>
        /// <param name="secretKey">Secret key entered by the operator.</param>
        /// <param name="error">
        /// Set to a diagnostic string when the check cannot be completed
        /// (e.g. file missing or JSON malformed); <c>null</c> on a clean run.
        /// </param>
        /// <returns><c>true</c> if credentials match; <c>false</c> otherwise.</returns>
        private static bool TryMatch(string username, string secretKey, out string? error)
        {
            error = null;

            if (!File.Exists(OverrideUsersPath))
            {
                error = $"override_users.json not found at: {OverrideUsersPath}";
                return false;
            }

            string json = File.ReadAllText(OverrideUsersPath).Trim();
            if (string.IsNullOrEmpty(json) || json == "[]")
            {
                error = "override_users.json contains no registered users.";
                return false;
            }

            List<OverrideUser>? users;
            try
            {
                users = JsonSerializer.Deserialize<List<OverrideUser>>(json);
            }
            catch (JsonException ex)
            {
                error = $"Failed to parse override_users.json — {ex.Message}";
                return false;
            }

            if (users is null || users.Count == 0)
            {
                error = "override_users.json contains no registered users.";
                return false;
            }

            return users.Exists(u =>
                string.Equals(u.Username, username, StringComparison.Ordinal) &&
                string.Equals(u.SecretKey, secretKey, StringComparison.Ordinal));
        }

        /// <summary>
        /// Creates (or overwrites) <c>Data/bypass.flag</c> with the username and
        /// a UTC timestamp. <c>LockdownManager.IsBypassed()</c> in the WinForms app
        /// polls for this file.
        /// </summary>
        private static void WriteBypassFlag(string username)
        {
            Directory.CreateDirectory(DataDir);

            File.WriteAllText(
                BypassFlagPath,
                $"bypassed_by={username}{Environment.NewLine}" +
                $"timestamp={DateTime.UtcNow:O}{Environment.NewLine}");
        }
    }
}
