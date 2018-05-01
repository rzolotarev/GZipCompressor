using Compressors;
using GZipCompressor.Archiever;
using GZipCompressor.Extensions;
using GZipCompressor.InputValidations;
using InputValidations;
using Outputs;
using Services.Decompressor;
using System;
using System.IO;

namespace GZipCompressor
{
    class Program
    {
        private static byte FAILURE_EXIT_CODE = 1;
        private static string END_PROCESS = "Press key to end process...";
        private static string PROCESS_CANCELLED = "Process was cancelled";
        private static string SUCCESS_CREATED = "File was successfully created";
        private static string FAILURE_CREATED = "File was not created";

        static void Main(string[] args)
        {
            GZipBlockArchiver compressor = null;

            Console.CancelKeyPress += (sender, ceArgs) =>
            {
                compressor.SetCancelStatus(true);                
                ConsoleLogger.WriteDiagnosticInfo(PROCESS_CANCELLED);                                
                Environment.Exit(FAILURE_EXIT_CODE);                
            };

            try
            {
                Validation.CheckInputParameters(args);               
                var inputFile = new FileInfo(args[1]);

                if (args[0] == Dictionary.COMPRESS_COMMAND)
                    compressor = new Compressor(args[1], args[2], inputFile.Length);
                else
                    compressor = new Decompressor(args[1], args[2], inputFile.Length);

                var threadManager = new ThreadManager();
                var processResult = compressor.Start(threadManager);

                Console.CursorTop += 1;
                if (processResult && !compressor.GetCancelStatus())                               
                    ConsoleLogger.WriteSuccessInfo($"{args[2]} : {SUCCESS_CREATED}");                
                else                
                    ConsoleLogger.WriteError($"{args[2]}: {FAILURE_CREATED}");                
            }
            catch(Exception ex)
            {
                ConsoleLogger.WriteError(FAILURE_CREATED);
                Console.WriteLine(END_PROCESS);
                Console.ReadKey();
            }
        }
    }
}
