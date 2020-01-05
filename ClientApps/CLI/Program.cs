using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using IO;
using LZ77;

namespace CLI
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var ifStream = new FileStream("sample.txt", FileMode.Open, FileAccess.Read);
            var reader = new BufferedReader(90, ifStream);
            var ofStream = new FileStream("output.lz77", FileMode.Create, FileAccess.Write);
            var writer = new BufferedWriter(90, ofStream);
            var compressor = await Compressor.Create(reader.ReadByte, writer.WriteCustomLength, 1024, 256);
            await compressor.Compress((ulong)ifStream.Length);
            await writer.FlushBuffer();
            ifStream.Close();
            ofStream.Close();

            ifStream = new FileStream("output.lz77", FileMode.Open, FileAccess.Read);
            reader = new BufferedReader(90, ifStream);
            ofStream = new FileStream("output.txt", FileMode.Create, FileAccess.Write);
            writer = new BufferedWriter(9, ofStream);
            var extr = new Extractor(reader.ReadCustomLength, writer.WriteCustomLength);
            await extr.Extract();
            await writer.FlushBuffer();
            ifStream.Close();
            ofStream.Close();
        }
    }
}
