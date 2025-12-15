using System;
using System.Diagnostics;
using System.IO;
using System.Text; // Важно для кодировки

class Program
{
    static void Main(string[] args)
    {
        // 1. ИСПРАВЛЕНИЕ КОДИРОВКИ (Чинит "?????")
        Console.OutputEncoding = Encoding.UTF8;

        // === НАСТРОЙКИ ===
        string gpgPath = @"C:\Program Files (x86)\GnuPG\bin\gpg.exe";
        string encryptedFile = @"C:\Users\pmiralamov\Downloads\Example.txt.gpg";
        string outputDir = @"C:\Users\pmiralamov\Downloads";
        string password = "";

        Console.WriteLine("=== ЗАПУСК GPG ЧЕРЕЗ C# ===");

        if (!File.Exists(gpgPath))
        {
            Console.WriteLine($"ОШИБКА: Не найден gpg.exe по пути: {gpgPath}");
            Console.ReadLine();
            return;
        }

        try
        {
            var gpg = new GpgService(gpgPath);
            string decryptedFile = gpg.DecryptFile(encryptedFile, outputDir, password);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"\n[УСПЕХ] Файл расшифрован!");
            Console.WriteLine($"Результат: {decryptedFile}");
            Console.ResetColor();
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\n[ОШИБКА]: {ex.Message}");
            Console.ResetColor();
        }

        Console.WriteLine("\nНажмите Enter, чтобы закрыть...");
        Console.ReadLine();
    }
}

public class GpgService
{
    private readonly string _gpgPath;

    public GpgService(string gpgPath)
    {
        _gpgPath = gpgPath;
    }

    public string DecryptFile(string inputFile, string outputFolder, string password = "")
    {
        if (!File.Exists(inputFile))
            throw new FileNotFoundException("Файл не найден", inputFile);

        string fileName = Path.GetFileName(inputFile);
        string targetName = fileName.EndsWith(".gpg") || fileName.EndsWith(".pgp")
            ? Path.GetFileNameWithoutExtension(fileName)
            : fileName + ".decrypted";

        string outputFile = Path.Combine(outputFolder, targetName);

        // Команда для GPG
        string arguments = $"--batch --yes --pinentry-mode loopback --passphrase-fd 0 --output \"{outputFile}\" --decrypt \"{inputFile}\"";

        var psi = new ProcessStartInfo
        {
            FileName = _gpgPath,
            Arguments = arguments,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8, // Важно для чтения логов GPG
            StandardErrorEncoding = Encoding.UTF8   // Важно для чтения ошибок GPG
        };

        using (var process = Process.Start(psi))
        {
            if (!string.IsNullOrEmpty(password))
            {
                process.StandardInput.WriteLine(password);
            }
            process.StandardInput.Flush();
            process.StandardInput.Close();

            string errorOutput = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new Exception($"Ошибка GPG: {errorOutput}");
            }
        }

        return outputFile;
    }
}