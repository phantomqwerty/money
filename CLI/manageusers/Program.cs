// ============================================================
//  ManageUsers — add, remove, and list students in
//  Data/users.json next to this exe.
//
//  users.json format (simple key/value map):
//    {
//      "StudentName": "password123",
//      ...
//    }
// ============================================================

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

string dataDir   = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
string usersFile = Path.Combine(dataDir, "users.json");

Directory.CreateDirectory(dataDir);

while (true)
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Cyan;
    Console.WriteLine("=== SEB User Manager ===");
    Console.ResetColor();
    Console.WriteLine("[1] List all students");
    Console.WriteLine("[2] Add a student");
    Console.WriteLine("[3] Remove a student");
    Console.WriteLine("[4] Exit");
    Console.Write("Choice: ");
    var choice = Console.ReadLine()?.Trim();

    switch (choice)
    {
        case "1":
            var users = LoadUsers();
            if (users.Count == 0) { Console.WriteLine("No students registered."); break; }
            Console.WriteLine("\nRegistered students:");
            foreach (var u in users) Console.WriteLine($"  - {u.Key}");
            break;

        case "2":
            Console.Write("Username: ");
            var newUser = Console.ReadLine()?.Trim();
            Console.Write("Password: ");
            var newPass = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(newUser) || string.IsNullOrEmpty(newPass))
            { Console.WriteLine("Username and password cannot be empty."); break; }
            var addUsers = LoadUsers();
            if (addUsers.ContainsKey(newUser))
            { Console.WriteLine($"User '{newUser}' already exists."); break; }
            addUsers[newUser] = newPass;
            SaveUsers(addUsers);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"   \u2713 Student '{newUser}' added.");
            Console.ResetColor();
            break;

        case "3":
            Console.Write("Username to remove: ");
            var delUser = Console.ReadLine()?.Trim();
            var delUsers = LoadUsers();
            if (string.IsNullOrEmpty(delUser) || !delUsers.ContainsKey(delUser))
            { Console.WriteLine($"User '{delUser}' not found."); break; }
            delUsers.Remove(delUser);
            SaveUsers(delUsers);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"   Student '{delUser}' removed.");
            Console.ResetColor();
            break;

        case "4":
            return;

        default:
            Console.WriteLine("Invalid choice.");
            break;
    }
}

Dictionary<string, string> LoadUsers()
{
    if (!File.Exists(usersFile)) return new Dictionary<string, string>();
    var json = File.ReadAllText(usersFile);
    return JsonSerializer.Deserialize<Dictionary<string, string>>(json)
           ?? new Dictionary<string, string>();
}

void SaveUsers(Dictionary<string, string> users)
{
    var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(usersFile, json);
}
