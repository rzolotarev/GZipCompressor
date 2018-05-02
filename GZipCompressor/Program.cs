using GZipCompressor.GZipArchiever;
using GZipCompressor.InputValidations;
using GZipCompressor.ProcessManagement;
using GZipCompressor.Service;
using GZipCompressor.Service.Archievers;
using GZipCompressor.Service.FileReaders;
using GZipCompressor.Service.FileWriters;
using InputValidations;
using Outputs;
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

                FileReader fileReader = null;
                FileWriter fileWriter = null;
                Archiever archiever = null;
                OperationType operationType = OperationType.Compress;
                GZipBlockArchiver compressor = new GZipBlockArchiver(args[1], args[2], threadManager, inputFile.Length);              
                
                if (args[0] == Dictionary.COMPRESS_COMMAND)
                {                    
                    fileReader = new UncompressedFileReader(args[1], compressor.CompressedDataManagers);
                    fileWriter = new CompressedFileWriter(args[2], compressor.DictionaryWritingManager);
                    archiever = new Compressor(compressor.CompressedDataManagers, compressor.DictionaryWritingManager);
                }
                else
                {                    
                    fileReader = new CompressedFileReader(args[1], compressor.CompressedDataManagers);
                    fileWriter = new DecompressedFileWriter(args[2], compressor.DictionaryWritingManager);
                    archiever = new Decompressor(compressor.CompressedDataManagers, compressor.DictionaryWritingManager);
                    operationType = OperationType.Decompress;
                }                            

                var processResult = compressor.Start(fileReader, archiever, fileWriter, operationType);

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
