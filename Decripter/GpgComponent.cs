using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace Decripter
{
    [Guid("D6F9C2A1-8E4B-4C3D-9F0A-1B2C3D4E5F6A")]
    public interface IGpgComponent
    {
        // 4-й параметр теперь: pathKeyFile (Путь к файлу ключа)
        string DecryptFile(string inputFile, string outputFile, string password, string pathKeyFile);
    }

    [Guid("A1B2C3D4-E5F6-7890-1234-567890ABCDEF")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("MyGpg.Wrapper")]
    [ComVisible(true)]
    public class GpgComponent : IGpgComponent
    {
        private const string GpgPath = @"C:\Program Files (x86)\GnuPG\bin\gpg.exe";
        private const string DefaultFolderPath = @"C:\Users\pmiralamov\Downloads\secret.asc";
        public string DecryptFile(string inputFile, string outputFile, string password, string pathKeyFile = DefaultFolderPath)
        {
            // Создаем уникальную временную папку для этой операции
            string tempHomeDir = Path.Combine(Path.GetTempPath(), "GPG_" + Guid.NewGuid().ToString());

            try
            {
                // 0. Проверки
                if (!File.Exists(pathKeyFile)) return "Ошибка: Файл ключа не найден: " + pathKeyFile;
                if (!File.Exists(GpgPath)) return "Ошибка: Не установлен Gpg4win";
                if (!File.Exists(inputFile)) return "Ошибка: Входящий файл не найден";

                Directory.CreateDirectory(tempHomeDir);

                // 1. ИМПОРТ КЛЮЧА во временную базу
                // --homedir указывает GPG использовать нашу временную папку
                string importArgs = string.Format("--homedir \"{0}\" --batch --import \"{1}\"", tempHomeDir, pathKeyFile);

                string importResult = RunGpg(importArgs);
                if (importResult != "OK") return "Ошибка импорта ключа: " + importResult;

                // 2. РАСШИФРОВКА
                // Тоже используем --homedir, чтобы GPG увидел ключ, который мы только что импортировали
                string decryptArgs = string.Format(
                    "--homedir \"{0}\" --batch --yes --pinentry-mode loopback --passphrase \"{1}\" --output \"{2}\" --decrypt \"{3}\"",
                    tempHomeDir, password, outputFile, inputFile);

                return RunGpg(decryptArgs);
            }
            catch (Exception ex)
            {
                return "Критическая ошибка: " + ex.Message;
            }
            finally
            {
                // 3. УБОРКА: Обязательно удаляем временную папку с ключами
                if (Directory.Exists(tempHomeDir))
                {
                    try { Directory.Delete(tempHomeDir, true); } catch { }
                }
            }
        }

        // Вспомогательный метод запуска процесса
        private string RunGpg(string arguments)
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = GpgPath,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            using (Process process = Process.Start(psi))
            {
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                // Код 0 = Успех
                if (process.ExitCode == 0) return "OK";

                // Иногда GPG пишет предупреждения в Error, но работает. 
                // Но если ExitCode != 0, это точно ошибка.
                return error;
            }
        }
    }
}