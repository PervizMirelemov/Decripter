using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Renci.SshNet;
using PgpCore;

class Program
{
    // --- НАСТРОЙКИ SFTP ---
    const string SftpHost = "sftp.example.com";
    const string SftpUser = "username";
    const string SftpPass = "password";
    const string RemotePath = "/incoming/";

    // --- НАСТРОЙКИ PGP ---
    const string PublicKeyPath = @"C:\Keys\public.asc";
    const string PrivateKeyPath = @"C:\Keys\private.asc";
    const string PrivateKeyPassword = ""; // Пароль, если есть

    // --- ЛОКАЛЬНЫЕ ПУТИ ---
    const string DownloadPath = @"C:\Work\Downloads";
    const string DecryptedPath = @"C:\Work\Decrypted";

    // ИСПРАВЛЕНИЕ: Используем обычный void Main для совместимости
    static void Main(string args)
    {
        // Запускаем асинхронную задачу синхронно
        MainAsync(args).GetAwaiter().GetResult();
    }

    static async Task MainAsync(string args)
    {
        Directory.CreateDirectory(DownloadPath);
        Directory.CreateDirectory(DecryptedPath);

        Console.WriteLine("Начинаю работу...");

        try
        {
            // 1. ПОДКЛЮЧЕНИЕ К SFTP
            // Проверьте, что сервер доступен, иначе тут упадет с ошибкой
            using (var client = new SftpClient(SftpHost, SftpUser, SftpPass))
            {
                try
                {
                    client.Connect();
                    Console.WriteLine($"Подключено к {SftpHost}");
                }
                catch
                {
                    Console.WriteLine("Не удалось подключиться к SFTP. Проверьте хост/логин/пароль.");
                    return;
                }

                var files = client.ListDirectory(RemotePath)
                                .Where(f => !f.IsDirectory && (f.Name.EndsWith(".pgp") || f.Name.EndsWith(".gpg") || f.Name.EndsWith(".asc")));

                foreach (var file in files)
                {
                    Console.WriteLine($"Обработка файла: {file.Name}");
                    string localEncryptedFile = Path.Combine(DownloadPath, file.Name);

                    // Формируем имя расшифрованного файла
                    string targetFileName = Path.GetFileNameWithoutExtension(file.Name);
                    if (!targetFileName.Contains(".")) targetFileName += ".txt";

                    string localDecryptedFile = Path.Combine(DecryptedPath, targetFileName);

                    // 2. СКАЧИВАНИЕ
                    using (Stream fileStream = File.Create(localEncryptedFile))
                    {
                        client.DownloadFile(file.FullName, fileStream);
                    }
                    Console.WriteLine($"-> Скачан в {localEncryptedFile}");

                    // 3. РАСШИФРОВКА
                    await DecryptFileAsync(localEncryptedFile, localDecryptedFile);
                }
                client.Disconnect();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Общая ошибка: {ex.Message}");
        }

        Console.WriteLine("Нажмите Enter для выхода...");
        Console.ReadLine();
    }

    static async Task DecryptFileAsync(string inputFile, string outputFile)
    {
        try
        {
            EncryptionKeys keys;
            if (string.IsNullOrEmpty(PrivateKeyPassword))
            {
                keys = new EncryptionKeys(new FileInfo(PrivateKeyPath), "");
            }
            else
            {
                keys = new EncryptionKeys(new FileInfo(PrivateKeyPath), PrivateKeyPassword);
            }

            PGP pgp = new PGP(keys);

            using (FileStream inputStream = File.OpenRead(inputFile))
            using (FileStream outputStream = File.Create(outputFile))
            {
                await pgp.DecryptAsync(inputStream, outputStream);
            }

            Console.WriteLine($"-> РАСШИФРОВАН УСПЕШНО: {outputFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"-> ОШИБКА PGP: {ex.Message}");
        }
    }
}