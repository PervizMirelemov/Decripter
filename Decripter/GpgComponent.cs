using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text; // Нужно для кодировок

namespace Decripter
{
    // Интерфейс для 1С
    [Guid("D6F9C2A1-8E4B-4C3D-9F0A-1B2C3D4E5F6A")]
    public interface IGpgComponent
    {
        string DecryptFile(string inputFile, string outputFile, string password);
    }

    // Основной класс
    [Guid("A1B2C3D4-E5F6-7890-1234-567890ABCDEF")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("MyGpg.Wrapper")] // Имя поменяли на Wrapper, чтобы не путать
    [ComVisible(true)]
    public class GpgComponent : IGpgComponent
    {
        // Путь к GPG обычно такой (проверьте у себя!)
        private const string GpgPath = @"C:\Program Files (x86)\GnuPG\bin\gpg.exe";

        public string DecryptFile(string inputFile, string outputFile, string password)
        {
            try
            {
                // Проверки
                if (!File.Exists(GpgPath)) return "Ошибка: Не установлен Gpg4win (нет gpg.exe)";
                if (!File.Exists(inputFile)) return "Ошибка: Входящий файл не найден";

                // Формируем аргументы для консольной команды
                // --batch --yes --pinentry-mode loopback позволяют вводить пароль без окон
                string arguments = string.Format(
                    "--batch --yes --pinentry-mode loopback --passphrase \"{0}\" --output \"{1}\" --decrypt \"{2}\"",
                    password, outputFile, inputFile);

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = GpgPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true, // Скрывает черное окно
                    StandardOutputEncoding = Encoding.UTF8, // Чтобы понимать кириллицу
                    StandardErrorEncoding = Encoding.UTF8
                };

                using (Process process = Process.Start(psi))
                {
                    // Читаем ошибки (если есть)
                    string errorOutput = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode == 0)
                    {
                        return "OK";
                    }
                    else
                    {
                        // Если ошибка - возвращаем то, что сказал GPG
                        return "Ошибка GPG: " + errorOutput;
                    }
                }
            }
            catch (Exception ex)
            {
                return "Критическая ошибка Wrapper: " + ex.Message;
            }
        }
    }
}