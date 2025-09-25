using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NFC_Reader_With_Service.Misc
{
    public class CardDetectedEventArgs : EventArgs
    {
        public string ReaderName { get; }
        public string CardUID { get; }
        public DateTime DetectedAt { get; }

        public CardDetectedEventArgs(string readerName, string cardUID)
        {
            ReaderName = readerName;
            CardUID = cardUID;
            DetectedAt = DateTime.Now;
        }
    }
}
