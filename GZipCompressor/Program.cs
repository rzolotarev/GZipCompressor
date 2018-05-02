using Compressors;
using GZipCompressor.Contracts;
using GZipCompressor.Extensions;
using GZipCompressor.GZipArchiever;
using GZipCompressor.InputValidations;
using GZipCompressor.ProcessManagement;
using GZipCompressor.Service;
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
            Console.CancelKeyPress += (sender, ceArgs) =>
            {
                StatusManager.ProcessIsCanceled = true;                
                ConsoleLogger.WriteDiagnosticInfo(PROCESS_CANCELLED);                                
                Environment.Exit(FAILURE_EXIT_CODE);                
            };

            try
            {
                Validation.CheckInputParameters(args);               
                var inputFile = new FileInfo(args[1]);

                var threadManager = new ThreadManager();

                GZipBlockArchiver compressor = null;
                if (args[0] == Dictionary.COMPRESS_COMMAND)
                    compressor = new Compressor(args[1], args[2],  threadManager, inputFile.Length);
                else
                    compressor = new Decompressor(args[1], args[2], threadManager , inputFile.Length);

                var fileReader = new FileReader(args[1], compressor.CompressedDataManagers);
                var archiever = new Archiever(compressor.CompressedDataManagers, compressor.DictionaryWritingManager);
                var fileWriter = new FileWriter(args[2], compressor.DictionaryWritingManager);

                var processResult = compressor.Start(fileReader, archiever, fileWriter);

                Console.CursorTop += 1;
                if (processResult && !StatusManager.ProcessIsCanceled)                               
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
