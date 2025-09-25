using NFC_Reader_With_Service.Misc;

namespace NFC_Reader_With_Service
{
    public class Program
    {
        private static INFCReaderService _nfcService;

        static void Main(string[] args)
        {
            _nfcService = new NFCReaderService();

            _nfcService.CardDetected += OnCardDetected;
            _nfcService.ErrorOccurred += OnErrorOccurred;

            if (_nfcService.StartMonitoring())
            {
                Console.WriteLine("NFC monitoring started. Place a card on the reader...");
                Console.WriteLine("Available readers:");
                foreach (var reader in _nfcService.GetAvailableReaders())
                {
                    Console.WriteLine($"- {reader}");
                }
            }
            else
            {
                Console.WriteLine("Failed to start the NFC monitoring.");
            }

            Console.WriteLine("Press any key to exit...");
            Console.ReadKey();

            _nfcService.StopMonitoring();
            _nfcService.Dispose();
        }

        private static void OnCardDetected(object sender, CardDetectedEventArgs e)
        {
            Console.WriteLine($"Card detected at {e.DetectedAt}");
            Console.WriteLine($"Reader: {e.ReaderName}");
            Console.WriteLine($"UID: {e.CardUID}");
            Console.WriteLine();
        }

        private static void OnErrorOccurred(object sender, string errorMessage)
        {
            Console.WriteLine($"Error: {errorMessage}");
        }
    }
}
