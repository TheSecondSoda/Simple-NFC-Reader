using PCSC;
using PCSC.Monitoring;
using System;
using System.Linq;

namespace NFC_Reader
{
    internal class Program
    {
        /*
         Hello! And welcome to Thomas notes about how this bullshit works!
        Please understand that these are my initial notes about how to use PCSC,
        so they are made to be understandable and readable, not in depth.
        The notes will also only cover lines with PCSC functions, not any other functions.
        Have fun :)
         */

        //For simplicities sake, and to get access to the actual reader quickly, this project will be made as a console app
        //Usually, all of this would probably be a service, but again, simplicity

        static void Main(string[] args)
        {
            /*
            The following using statement makes a connection to the hosts PC/SC system service, 
            which is basically the subsystem that handle all of the computers smart card readers.
            Just plug in a NFC reader, and it will dynamically pop up in the services list.
            */
            using (var context = ContextFactory.Instance.Establish(SCardScope.System))
            {
                //The context has a function that lets us find all of the names of currently connected card readers
                //This makes it easy to later pick what reader we want to use, if multiple are plugged in,
                //or in case we need different handling for different models
                //The function will return an array of strings
                var readerNames = context.GetReaders();

                if (readerNames == null || !readerNames.Any())
                {
                    Console.WriteLine("No smart card readers found");
                    return;
                }

                Console.WriteLine("Readers found on PC/SC system service:");
                foreach (var readerName in readerNames)
                {
                    Console.WriteLine($"- {readerName}");
                }

                //I guess one could just constantly check if there is a card? But that sounds awfully inefficient.
                //Instead, the PCSC package provides us with monitors! Quite conveniently, they can keep look for us,
                //as well as call a specified function when a card does get close enough to the scanner
                var eyeOfSauron = MonitorFactory.Instance.Create(SCardScope.System);
                //I have set it to look for when a card is inserted, but it is also possible to set it to look for when a card is removed
                eyeOfSauron.CardInserted += OnCardInserted;
                eyeOfSauron.Start(readerNames.First()); //Uses first reader, no need to make it more complex

                Console.WriteLine("Awaiting cards... Press enter to exit.");
                Console.ReadLine();
            }
        }

        //Oh my! Another new class type? The CardStatusEventArgs is the class containing all of the data from the read card
        private static void OnCardInserted(object sender, CardStatusEventArgs e)
        {
            Console.WriteLine($"Card detected by reader: {e.ReaderName}");

            try
            {
                //See line 28
                using (var context = ContextFactory.Instance.Establish(SCardScope.System))
                //This line connects to the specific reader that detected the card, as we need some info from it.
                //It does however have two interesting lines:
                // - SCardShareMode.Shared allows other applications to also access the card
                // - SCardProtocol.Any accepts any communication protocol the card supports, we aren't picky in this example
                using (var reader = context.ConnectReader(e.ReaderName, SCardShareMode.Shared, SCardProtocol.Any))
                {
                    //According to the internet, most student cards quite simply use the cards UID as the studentID
                    //Well just assume thats true for our case
                    var uid = GetCardUID(reader);
                    Console.WriteLine($"Student ID: {BitConverter.ToString(uid).Replace("-", "")}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error reading card: {ex.Message}");
            }
        }

        private static byte[] GetCardUID(ICardReader reader)
        {
            //The student card UCL uses is of the ISO 14443-3A standard. However, one can't simply read the code from such a card.
            //We need an APDU (Application Protocol Data Unit) command. Basically a standardized command format for smart cards.
            var command = new byte[] { 0xFF, 0xCA, 0x00, 0x00, 0x00 };
            //Those were a lot of bytes, but what do they actually mean?
            // - First: 0xFF is the class byte. It indicates that the byte array is a "vendor command". 
            // - Second: 0xCA is the instruction byte. In this case it tells the card that it's a read command.
            // - Third and fourth: 0x00 are the first and second parameters, don't think we need them for this example.
            // - Fifth: 0x00 is called Le. It tells us the expected length of the read response. Writing 0x00 tells it to give us the full response, no matter the length.
            // The FF CA 00 00 00 is the standard command for getting the cards UID
            var response = new byte[256];
            //The transmit function will send it's first parameter as a command, and store the response in it's second parameter
            //The function itself will also return the length of the response
            var recievedLength = reader.Transmit(command, response);

            //A response from a smart card will always end with two status bytes, SW1 and SW2
            //If SW1 is 0x90 and SW2 is 0x00, it means "success", anything else is an error
            if (recievedLength >= 2 && response[recievedLength - 2] == 0x90 && response[recievedLength - 1] == 0x00)
            {
                var uid = new byte[recievedLength - 2];
                Array.Copy(response, 0, uid, 0, recievedLength - 2);
                return uid;
            }

            throw new Exception("Failed to read the card UID");
        }
    }
}
