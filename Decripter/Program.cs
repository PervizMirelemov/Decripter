using System;
using System.IO;
using System.Threading.Tasks;
using PgpCore; // Нужен только этот пакет

class Program
{
    static async Task Main(string args)
    {
        // 1. НАСТРОЙКИ
        // Путь к вашему зашифрованному файлу (ИЗМЕНИТЕ ЭТО НА ВАШ ФАЙЛ)
        string inputFile = @"C:\Users\pmiralamov\Downloads\Example.txt.gpg"; 
        
        // Куда сохранить результат
        string outputFile = @"C:\Users\pmiralamov\Downloads";

        // Пути к ключам
        string privateKeyPath = @"C:\Users\pmiralamov\Downloads\public.asc";
        string publicKeyPath = @"C:\Users\pmiralamov\Downloads\SECRET.asc";
        string password = ""; // Оставьте пустым, если пароля нет

        Console.WriteLine($"Расшифровываю файл: {inputFile}");

        try
        {
            // Проверки, чтобы не гадать, почему упало
            if (!File.Exists(inputFile))
            {
                Console.WriteLine("ОШИБКА: Зашифрованный файл не найден!");
                return;
            }
            if (!File.Exists(privateKeyPath))
            {
                Console.WriteLine("ОШИБКА: Не найден файл приватного ключа!");
                return;
            }

            // 2. ИНИЦИАЛИЗАЦИЯ PGP
            // Загружаем ключи. Если пароль пустой, библиотека попробует открыть ключ без него.
            EncryptionKeys keys;
            if (string.IsNullOrEmpty(password))
            {
                keys = new EncryptionKeys(new FileInfo(publicKeyPath), new FileInfo(privateKeyPath), "");
            }
            else
            {
                keys = new EncryptionKeys(new FileInfo(publicKeyPath), new FileInfo(privateKeyPath), password);
            }

            PGP pgp = new PGP(keys);

            // 3. РАСШИФРОВКА
            // Используем потоки для надежности
            using (FileStream inputStream = File.OpenRead(inputFile))
            using (FileStream outputStream = File.Create(outputFile))
            {
                await pgp.DecryptAsync(inputStream, outputStream);
            }

            Console.WriteLine("УСПЕХ! Файл расшифрован.");
            Console.WriteLine($"Результат здесь: {outputFile}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ОШИБКА: {ex.Message}");
            if (ex.Message.Contains("Checksum mismatch"))
            {
                Console.WriteLine("ПОДСКАЗКА: Ошибка 'Checksum mismatch' означает, что ключ ЗАПАРОЛЕН, а вы не указали пароль.");
            }
        }

        Console.ReadLine();
    }
}