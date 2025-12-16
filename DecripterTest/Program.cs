using System;
using System.Text;
using Decripter; // Ссылка на ваш проект DLL

class Program
{
    static void Main(string[] args)
    {
        // 1. Исправляем "вопросики" в консоли
        Console.OutputEncoding = Encoding.UTF8;
        Console.WriteLine("=== ЗАПУСК ТЕСТА (WRAPPER) ===");

        GpgComponent comp = new GpgComponent();

        // 2. Ваши пути (проверьте их!)
        string inFile = @"C:\Users\pmiralamov\Downloads\Example.txt.gpg";
        
        string outFile = inFile.Replace(".gpg", "");

        string pass = ""; // Пароль, если есть

        // Вызов
        Console.WriteLine("Начинаю расшифровку...");
        // Заметьте: в Wrapper нам не нужны пути к ключам в параметрах, 
        // так как Kleopatra сама их найдет в своей базе.
        string result = comp.DecryptFile(inFile, outFile, pass);

        Console.WriteLine("Результат: " + result);
        Console.ReadLine();
    }
}