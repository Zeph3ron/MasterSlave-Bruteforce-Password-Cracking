using System;
using System.Collections.Generic;
using System.IO;

namespace BruteForceMaster
{
    internal class DictionaryHandler
    {
        private readonly int _nrOfSubsets;
        private int _lastSet;
        public int SetsReceived { get; set; }
        private readonly object _lock = new object();

        public DictionaryHandler(int nrOfSubsets)
        {
            _nrOfSubsets = nrOfSubsets;
            _lastSet = 0;
            SetsReceived = 0;
            ChunkDownDictionary();
        }

        public FileStream GetDictionarySubset()
        {
            lock (_lock)
            {
                var fs = new FileStream("dictionaryPart" + _lastSet + ".txt", FileMode.Open, FileAccess.Read);
                int setToDisplay = _lastSet + 1;
                Console.WriteLine("Server just sent set nr:(" + setToDisplay + " of " + _nrOfSubsets + ")");
                _lastSet++;
                return fs;
            }
        }

        public void ChunkDownDictionary()
        {
            var fs = new FileStream("webster-dictionary.txt", FileMode.Open, FileAccess.Read);
            var streamWriters = new List<StreamWriter>();
            for (var i = 0; i < _nrOfSubsets; i++)
            {
                streamWriters.Add(
                    new StreamWriter(new FileStream("dictionaryPart" + i + ".txt", FileMode.Create, FileAccess.ReadWrite)));
            }
            using (var streamReader = new StreamReader(fs))
            {
                //Should stop when the stream has finished
                while (!streamReader.EndOfStream)
                {
                    foreach (var writer in streamWriters)
                    {
                        writer.WriteLine(streamReader.ReadLine());
                    }
                }
                foreach (var writer in streamWriters)
                {
                    writer.Dispose();
                }
            }
        }

        public void CleanUp()
        {
            for (int i = 0; i < _nrOfSubsets; i++)
            {
                System.IO.File.Delete("dictionaryPart" + i + ".txt");
            }
        }

        public bool IsWorkDone()
        {
            if (_lastSet == _nrOfSubsets && SetsReceived == _nrOfSubsets)
            {
                return true;
            }
            return false;
        }
    }
}