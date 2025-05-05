using System;
using System.IO;
using System.Linq;
using System.Text;

namespace UFontExtractor
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.InputEncoding = Encoding.UTF8;

            foreach (string arg in args)
            {
                try
                {
                    ProcessUFont(arg); 
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine($"[✓] Successfully saved: {Path.ChangeExtension(arg, ".ttf")}");
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"[✗] Error: {ex.Message}", ex);
                }
                finally
                {
                    Console.ResetColor();
                }
            }

            for (int i = 3; i > 0; i--)
            {
                Console.WriteLine($"Closing in {i} seconds...");
                System.Threading.Thread.Sleep(1000);
            }
        }

        static void ProcessUFont(string path)
        {
            if (Directory.Exists(path))
                throw new FileNotFoundException("Is not a file.", path);

            if (!File.Exists(path))
                throw new FileNotFoundException("File not found.", path);

            if (!path.EndsWith(".ufont", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("File is not a UFont.");

            Console.WriteLine($"Reading: {path}");

            byte[] data = File.ReadAllBytes(path);
            int ttfStart = FindTtfStart(data);
            if (ttfStart < 0)
                throw new InvalidDataException("TTF signature not found in file.");

            int ttfLength = GetTtfLength(data, ttfStart);
            if (ttfLength <= 0)
                throw new InvalidDataException("Failed to determine TTF length.");

            byte[] ttfData = data.Skip(ttfStart).Take(ttfLength).ToArray();
            string outPath = Path.ChangeExtension(path, ".ttf");
            File.WriteAllBytes(outPath, ttfData);
        }

        static int FindTtfStart(byte[] data)
        {
            for (int i = 0; i < data.Length - 4; i++)
            {
                if (data[i] == 0x00 && data[i + 1] == 0x01 && data[i + 2] == 0x00 && data[i + 3] == 0x00)
                    return i;
                if (data[i] == 'O' && data[i + 1] == 'T' && data[i + 2] == 'T' && data[i + 3] == 'O')
                    return i;
            }
            return -1;
        }

        static int GetTtfLength(byte[] data, int start)
        {
            if (start + 12 >= data.Length)
                throw new InvalidDataException("Insufficient data for TTF header.");

            ushort numTables = (ushort)((data[start + 4] << 8) | data[start + 5]);
            int offset = start + 12;
            int max = 0;

            for (int i = 0; i < numTables; i++)
            {
                int entry = offset + i * 16;
                if (entry + 16 > data.Length)
                    throw new InvalidDataException("Corrupted table entry in font.");

                int tableOffset = ToInt32BE(data, entry + 8);
                int tableLength = ToInt32BE(data, entry + 12);
                max = Math.Max(max, tableOffset + tableLength);
            }

            return max;
        }

        static int ToInt32BE(byte[] data, int index)
        {
            return (data[index] << 24) |
                   (data[index + 1] << 16) |
                   (data[index + 2] << 8) |
                   data[index + 3];
        }
    }
}
