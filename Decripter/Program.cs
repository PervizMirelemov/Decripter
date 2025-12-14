using System;
using System.IO;
using System.Linq;
using System.Text; // Важно для кодировки
using System.Threading.Tasks;
using Renci.SshNet; // Библиотека SFTP
using PgpCore;      // Библиотека PGP

    class Program
    {
        // ==========================================
        // 1. НАСТРОЙКИ (ДЛЯ ВАШЕГО ЛОКАЛЬНОГО ТЕСТА)
        // ==========================================
        // Данные берем из окна Rebex Tiny SFTP Server
        const string SftpHost = "127.0.0.1";
        const int SftpPort = 22;
        const string SftpUser = "tester";
        const string SftpPass = "password";
        const string RemotePath = "/"; // Корень сервера

        // ПУТИ К ВАШИМ КЛЮЧАМ (Проверьте, что файлы существуют!)
        const string PrivateKeyPath = @"C:\Keys\private.asc";
        const string PublicKeyPath = @"C:\Keys\public.asc";

        // Если ключ создавали без пароля - оставьте пустым
        const string PrivateKeyPassword = "";

        // ПАПКИ НА ВАШЕМ КОМПЬЮТЕРЕ
        const string DownloadPath = @"C:\Work\Downloads";
        const string DecryptedPath = @"C:\Work\Decrypted";

        // ==========================================
        // 2. ГЛАВНАЯ ТОЧКА ВХОДА
        // ==========================================
        // Используем классический Main во избежание ошибок компилятора
        static void Main(string args)
        {
            // ИСПРАВЛЕНИЕ КОДИРОВКИ (Чинит "?????")
            Console.OutputEncoding = Encoding.UTF8;

            Console.WriteLine("=== ЗАПУСК СИСТЕМЫ ПОЛУЧЕНИЯ ФАЙЛОВ ===");

            // Запуск асинхронной задачи в синхронном режиме
            try
            {
                MainAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n: {ex.Message}");
            }

            Console.WriteLine("\nНажмите Enter, чтобы закрыть окно...");
            Console.ReadLine();
        }

        static async Task MainAsync()
        {
            // Создаем рабочие папки, чтобы не было ошибки "DirectoryNotFound"
            Directory.CreateDirectory(DownloadPath);
            Directory.CreateDirectory(DecryptedPath);

            // Проверка наличия ключа перед стартом
            if (!File.Exists(PrivateKeyPath))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"[ОШИБКА] Не найден файл приватного ключа по пути: {PrivateKeyPath}");
                Console.ResetColor();
                return; // Останавливаем работу, так как без ключа нет смысла продолжать
            }

            // БЛОК 1: ПОДКЛЮЧЕНИЕ И СКАЧИВАНИЕ
            using (var client = new SftpClient(SftpHost, SftpPort, SftpUser, SftpPass))
            {
                try
                {
                    Console.Write($"Подключение к {SftpHost}... ");
                    client.Connect();
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("УСПЕХ");
                    Console.ResetColor();
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"\n[ОШИБКА ПОДКЛЮЧЕНИЯ]: {ex.Message}");
                    Console.WriteLine("Совет: Убедитесь, что Rebex Tiny SFTP Server запущен.");
                    Console.ResetColor();
                    return;
                }

                // Поиск файлов
                var files = client.ListDirectory(RemotePath)
                                 .Where(f => !f.IsDirectory &&
                                              (f.Name.EndsWith(".pgp") || f.Name.EndsWith(".gpg") || f.Name.EndsWith(".asc")));

                foreach (var file in files)
                {
                    Console.WriteLine($"\nНайден файл: {file.Name}");

                    string localEncryptedFile = Path.Combine(DownloadPath, file.Name);

                    // Скачивание
                    using (Stream fileStream = File.Create(localEncryptedFile))
                    {
                        client.DownloadFile(file.FullName, fileStream);
                    }
                    Console.WriteLine($" -> Скачан в: {localEncryptedFile}");

                    // Формирование имени для расшифрованного файла
                    // Пример: report.txt.pgp -> report.txt
                    string targetName = Path.GetFileNameWithoutExtension(file.Name);
                    // Если исходный файл был просто "data.pgp", то targetName станет "data". Добавим.txt на всякий случай.
                    if (!targetName.Contains(".")) targetName += ".txt";

                    string localDecryptedFile = Path.Combine(DecryptedPath, targetName);

                    // БЛОК 2: ДЕШИФРОВКА
                    await DecryptFileAsync(localEncryptedFile, localDecryptedFile);
                }

                client.Disconnect();
            }
        }

        static async Task DecryptFileAsync(string inputFile, string outputFile)
        {
            try
            {
                EncryptionKeys keys;
                // Логика обработки "пустого" пароля
                if (string.IsNullOrEmpty(PrivateKeyPassword))
                {
                    keys = new EncryptionKeys(new FileInfo(PrivateKeyPath), "");
                }
                else
                {
                    keys = new EncryptionKeys(new FileInfo(PrivateKeyPath), PrivateKeyPassword);
                }

                PGP pgp = new PGP(keys);

                using (FileStream inp = File.OpenRead(inputFile))
                using (FileStream outp = File.Create(outputFile))
                {
                    await pgp.DecryptAsync(inp, outp);
                }

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($" -> РАСШИФРОВАН: {outputFile}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($" -> ОШИБКА PGP: {ex.Message}");
                if (ex.Message.Contains("Checksum mismatch"))
                {
                    Console.WriteLine("    [!] Ключ требует пароль, который не был указан или неверен.");
                }
                Console.ResetColor();
            }
        }
    }
