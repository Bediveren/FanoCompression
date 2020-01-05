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
            // --Encoding --
            byte wordLength = 16;  
            
            FileStream readFileStream = new FileStream("../../../sample.zip", FileMode.Open, FileAccess.Read);
            BufferedReader reader = new BufferedReader(5_000_000, readFileStream);

            FileStream writeFileStream = new FileStream("../../../sample.fano", FileMode.Create, FileAccess.Write);
            BufferedWriter writer = new BufferedWriter(5_000_000, writeFileStream);

            var fano = new FanoEncoder(reader , writer);
            await fano.Encode(wordLength);

            await writer.FlushBuffer();

            readFileStream.Close(); 
            writeFileStream.Close();
            

            
            FileStream readFileStream2 = new FileStream("../../../sample.fano", FileMode.Open, FileAccess.Read);
            BufferedReader reader2 = new BufferedReader(5_000_000, readFileStream2);

           
            FileStream writeFileStream2 = new FileStream("../../../sampleDecoded.zip", FileMode.Create, FileAccess.Write);
            BufferedWriter writer2 = new BufferedWriter(5_000_000, writeFileStream2);

            var fano2 = new FanoEncoder(reader2, writer2);
            await fano2.Decode();

            await writer2.FlushBuffer();


            readFileStream2.Close();
            writeFileStream2.Close();
            

        }
    }
}
