using System;
using System.IO;

public class Program
{
    public static void Main(string[] args)
    {
        // Config();
        Solve(Console.In, Console.Out);
    }

    public static void Config()
    {
        string input = "5\n1 2 3 4 5\n";
        using var reader = new StringReader(input);
        using var writer = new StringWriter();
        Solve(reader, writer);
        Console.WriteLine(writer.ToString());
    }

    public static void Solve(TextReader reader, TextWriter writer)
    {
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            writer.WriteLine(line);
        }
    }
}
