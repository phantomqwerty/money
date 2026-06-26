using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            var store = LoadStore();
            if (store.students.Count == 0) { Console.WriteLine("No students registered."); break; }
            Console.WriteLine("\nRegistered students:");
            foreach (var s in store.students) Console.WriteLine($"  - {s.username}");
            break;

        case "2":
            Console.Write("Username: ");
            var newUser = Console.ReadLine()?.Trim();
            Console.Write("Secret Key: ");
            var newPass = Console.ReadLine()?.Trim();
            if (string.IsNullOrEmpty(newUser) || string.IsNullOrEmpty(newPass))
            { Console.WriteLine("Username and secret key cannot be empty."); break; }
            var addStore = LoadStore();
            if (addStore.students.Exists(s => s.username == newUser))
            { Console.WriteLine($"User '{newUser}' already exists."); break; }
            addStore.students.Add(new Student { username = newUser, secretKey = newPass });
            SaveStore(addStore);
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"   \u2713 Student '{newUser}' added.");
            Console.ResetColor();
            break;

        case "3":
            Console.Write("Username to remove: ");
            var delUser = Console.ReadLine()?.Trim();
            var delStore = LoadStore();
            var toRemove = delStore.students.FindIndex(s => s.username == delUser);
            if (string.IsNullOrEmpty(delUser) || toRemove < 0)
            { Console.WriteLine($"User '{delUser}' not found."); break; }
            delStore.students.RemoveAt(toRemove);
            SaveStore(delStore);
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

UserStore LoadStore()
{
    if (!File.Exists(usersFile)) return new UserStore();
    var json = File.ReadAllText(usersFile);
    return JsonSerializer.Deserialize<UserStore>(json) ?? new UserStore();
}

void SaveStore(UserStore store)
{
    var json = JsonSerializer.Serialize(store, new JsonSerializerOptions { WriteIndented = true });
    File.WriteAllText(usersFile, json);
}

class Student  { public string username { get; set; } = ""; public string secretKey { get; set; } = ""; }
class UserStore { public List<Student> students { get; set; } = new(); }
