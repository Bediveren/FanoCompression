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
            var fStream = new FileStream("sample.zip", FileMode.Open);
            var reader = new BufferedReader(6000000, fStream);
            var compr = await Compressor.Create(reader.ReadByte, (x) => { }, 1024, 256);
            await compr.Compress((ulong)fStream.Length);
        }
    }
}
