using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace aclogview.Tools.Scrapers
{
    class PacketFragmentsPerPacketScraper : Scraper
    {
        public override string Description => "Generates stats to show the number of fragments per packet";

        private readonly ConcurrentDictionary<int, uint> c2sHitsPerFragmentCount = new ConcurrentDictionary<int, uint>();
        private readonly ConcurrentDictionary<int, uint> s2cHitsPerFragmentCount = new ConcurrentDictionary<int, uint>();

        public override void Reset()
        {
            c2sHitsPerFragmentCount.Clear();
            s2cHitsPerFragmentCount.Clear();
        }

        /// <summary>
        /// This can be called by multiple thread simultaneously
        /// </summary>
        public override (int hits, int messageExceptions) ProcessFileRecords(string fileName, List<PacketRecord> records, ref bool searchAborted)
        {
            int hits = 0;
            int messageExceptions = 0;

            foreach (PacketRecord record in records)
            {
                if (searchAborted)
                    return (hits, messageExceptions);

                ConcurrentDictionary<int, uint> workingDict;

                if (record.isSend)
                    workingDict = c2sHitsPerFragmentCount;
                else
                    workingDict = s2cHitsPerFragmentCount;

                if (!workingDict.ContainsKey(record.frags.Count))
                    workingDict.TryAdd(record.frags.Count, 0);

                workingDict[record.frags.Count]++;
            }

            return (hits, messageExceptions);
        }

        public override void WriteOutput(string destinationRoot, ref bool writeOuptputAborted)
        {
            var sb = new StringBuilder();

            sb.AppendLine("Client to Server");

            var sortedKeys = c2sHitsPerFragmentCount.Keys.ToList();
            sortedKeys.Sort();

            var sum = c2sHitsPerFragmentCount.Sum(r => r.Value);

            foreach (var key in sortedKeys)
                sb.AppendLine($"Fragment count: {key.ToString().PadLeft(4)}, hits: {c2sHitsPerFragmentCount[key].ToString("N0").PadLeft(11)}, percent vs total: {(((double)c2sHitsPerFragmentCount[key] / sum) * 100).ToString("N2").PadLeft(6)}");

            sb.AppendLine();
            sb.AppendLine("Server to Client");

            sortedKeys = s2cHitsPerFragmentCount.Keys.ToList();
            sortedKeys.Sort();

            sum = s2cHitsPerFragmentCount.Sum(r => r.Value);

            foreach (var key in sortedKeys)
                sb.AppendLine($"Fragment count: {key.ToString().PadLeft(4)}, hits: {s2cHitsPerFragmentCount[key].ToString("N0").PadLeft(11)}, percent vs total: {(((double)s2cHitsPerFragmentCount[key] / sum) * 100).ToString("N2").PadLeft(6)}");


            var fileName = GetFileName(destinationRoot);
            File.WriteAllText(fileName, sb.ToString());
        }
    }
}
