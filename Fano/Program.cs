using IO;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace FanoCompression
{
    class Program
    {

        static async Task Main(string[] args)
        {
            int wordLength = 8;
                
            FileStream readFileStream = new FileStream("../../../sample.txt", FileMode.Open, FileAccess.Read);
            BufferedReader reader = new BufferedReader(8000, readFileStream);

            FileStream writeFileStream = new FileStream("../../../sample2.fano", FileMode.Create, FileAccess.Write);
            BufferedWriter writer = new BufferedWriter(8000, writeFileStream);

            var fano = new FanoEncoder(reader , writer, wordLength);
            await fano.Encode();

            readFileStream.Close();
            writeFileStream.Close();

        }
    }
}
