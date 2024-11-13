using System;
using System.IO;
using System.Linq;
using System.Globalization;

class Program
{
    public static void Main()
    {
        string projectDir = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
        string incomingPath = Path.Combine(projectDir, "Incoming", "InputData.txt");
        string outgoingPath = Path.Combine(projectDir, "Outgoing", "OutputData.csv");
        string backupDir = Path.Combine(projectDir, "Backup");

        // Crear Dirctorios 
        Directory.CreateDirectory(Path.Combine(projectDir, "Incoming"));
        Directory.CreateDirectory(Path.Combine(projectDir, "Outgoing"));
        Directory.CreateDirectory(backupDir);

        Console.WriteLine("Path: " + Path.GetFullPath(incomingPath));

        // Verifica si el archivo de entrada existe
        if (File.Exists(incomingPath))
        {
            try
            {
                // Lee el archivo de entrada
                var inputData = File.ReadAllLines(incomingPath);

                if (inputData.Length == 0)
                {
                    Console.WriteLine("El archivo de entrada está vacío.");
                    return;
                }

                // Procesa el archivo y genera contenido CSV
                string[] outputData = ProcessInputData(inputData);

                // Crea el archivo CSV de salida
                File.WriteAllLines(outgoingPath, outputData);

                // Guarda copias en el directorio de respaldo
                File.Copy(incomingPath, Path.Combine(backupDir, "InputData_backup.txt"), true);
                File.Copy(outgoingPath, Path.Combine(backupDir, "OutputData_backup.csv"), true);

                Console.WriteLine("Archivo procesado y guardado exitosamente.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ocurrió un error: {ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("El archivo de entrada no existe en la carpeta Incoming.");
        }
    }

    static string[] ProcessInputData(string[] lines)
    {
        // Obtiene el encabezado de la primera línea
        var header = lines[0].Replace('|', ','); // Reemplaza '|' con ',' para formato CSV
        var outputLines = new System.Collections.Generic.List<string> { header }; // Inicia la salida con el encabezado

        // Procesa el registro de cada cliente
        for (int i = 1; i < lines.Length; i++) // Comienza desde la segunda línea
        {
            var columns = lines[i].Split('|'); // Divide por '|'

            // Crea el CUSTOMER_RECORD dinámicamente
            var customerRecord = $"CUSTOMER_RECORD,{string.Join(",", columns.Select(c => $"\"{c}\""))}";

            outputLines.Add(customerRecord);

            // Registros de detalles (se asume que las cantidades están en índices pares y los detalles en índices impares)
            for (int j = 10; j < columns.Length; j += 2) // Comienza desde 'details1'
            {
                if (j + 1 < columns.Length) // Asegura que hay una cantidad para el detalle
                {
                    if (decimal.TryParse(columns[j + 1], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                    {
                        string code = GetCodeForAmount(amount);
                        var detailsRecord = $"DETAILS_RECORD,{string.Join(",", new[] { $"\"{columns[j]}\"", $"\"{code}\"", $"\"${amount:F2}\"" })}"; // Formatea la cantidad a 2 decimales
                        outputLines.Add(detailsRecord);
                    }
                }
            }

            // Agrega una línea de total para cada cliente (si es necesario)
            decimal totalAmount = 0;
            for (int j = 11; j < columns.Length; j += 2) // Comienza desde 'amount1'
            {
                if (j < columns.Length && decimal.TryParse(columns[j], NumberStyles.Any, CultureInfo.InvariantCulture, out decimal amount))
                {
                    totalAmount += amount;
                }
            }
            outputLines.Add($"DETAILS_RECORD,\"TOTAL\",\"${totalAmount:F2}\""); // Línea de total
        }

        return outputLines.ToArray();
    }

    static string GetCodeForAmount(decimal amount)
    {
        var thresholds = new (decimal min, decimal max, string code)[]
        {
            (decimal.MinValue, 500, "N"),
            (500, 1000, "A"),
            (1000, 1500, "C"),
            (1500, 2000, "L"),
            (2000, 2500, "P"),
            (2500, 3000, "X"),
            (3000, 5000, "T"),
            (5000, 10000, "S"),
            (10000, 20000, "U"),
            (20000, 30000, "R"),
            (30000, decimal.MaxValue, "V")
        };

        string temp = null;

        foreach (var (min, max, code) in thresholds)
        {
            if (amount > min && amount <= max)
            {
                temp = code;
                break;
            }
        }

        if (temp == null) temp = "ERROR";

        return temp;
    }
}
