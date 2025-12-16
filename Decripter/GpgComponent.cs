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
        // Вернул параметр по умолчанию в интерфейс
        string DecryptFile(string inputFile, string outputFile, string password, string pathKeyFile = GpgComponent.DefaultKeyPath);
    }

    [Guid("A1B2C3D4-E5F6-7890-1234-567890ABCDEF")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("MyGpg.Wrapper")]
    [ComVisible(true)]
    public class GpgComponent : IGpgComponent
    {
        // Константы
        private const string GpgPath = @"C:\GnuPG\bin\gpg.exe";

        // Сделал константу публичной, чтобы интерфейс её видел
        public const string DefaultKeyPath = @"C:\Keys\secret.asc";

        // ВЕРНУЛ: pathKeyFile = DefaultKeyPath
        public string DecryptFile(string inputFile, string outputFile, string password, string pathKeyFile = DefaultKeyPath)
        {
            // ЗАЩИТА: Если 1С передаст пустую строку "" (так часто бывает),
            // мы все равно принудительно ставим путь по умолчанию.
            if (string.IsNullOrWhiteSpace(pathKeyFile))
            {
                pathKeyFile = DefaultKeyPath;
            }

            string tempHomeDir = Path.Combine(Path.GetTempPath(), "GPG_" + Guid.NewGuid().ToString());

            try
            {
                // Проверки
                if (!File.Exists(pathKeyFile)) return "Ошибка: Файл ключа не найден по пути: " + pathKeyFile;
                if (!File.Exists(GpgPath)) return "Ошибка: Не установлен Gpg4win";
                if (!File.Exists(inputFile)) return "Ошибка: Входящий файл не найден: " + inputFile;

                // Умное имя файла (File_1.xml)
                string finalOutputPath = GetUniqueFilePath(outputFile);

                Directory.CreateDirectory(tempHomeDir);

                // Импорт ключа
                string importArgs = string.Format("--homedir \"{0}\" --batch --import \"{1}\"", tempHomeDir, pathKeyFile);
                string importResult = RunGpg(importArgs);

                if (importResult != "OK") return "Ошибка импорта ключа: " + importResult;

                // Расшифровка
                string decryptArgs = string.Format(
                    "--homedir \"{0}\" --batch --yes --pinentry-mode loopback --passphrase \"{1}\" --output \"{2}\" --decrypt \"{3}\"",
                    tempHomeDir, password, finalOutputPath, inputFile);

                string decryptResult = RunGpg(decryptArgs);

                if (decryptResult == "OK")
                {
                    // Возвращаем новое имя файла
                    return "OK:" + finalOutputPath;
                }
                else
                {
                    return decryptResult;
                }
            }
            catch (Exception ex)
            {
                return "Критическая ошибка C#: " + ex.Message;
            }
            finally
            {
                if (Directory.Exists(tempHomeDir))
                {
                    try { Directory.Delete(tempHomeDir, true); } catch { }
                }
            }
        }

        private string GetUniqueFilePath(string fullPath)
        {
            if (!File.Exists(fullPath)) return fullPath;

            string directory = Path.GetDirectoryName(fullPath);
            string fileNameOnly = Path.GetFileNameWithoutExtension(fullPath);
            string extension = Path.GetExtension(fullPath);
            int counter = 1;
            string newPath = fullPath;

            while (File.Exists(newPath))
            {
                newPath = Path.Combine(directory, string.Format("{0}_{1}{2}", fileNameOnly, counter, extension));
                counter++;
            }
            return newPath;
        }

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
                if (process.ExitCode == 0) return "OK";
                return string.IsNullOrWhiteSpace(error) ? "Ошибка GPG (код " + process.ExitCode + ")" : error;
            }
        }
    }
}