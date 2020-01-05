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
            byte wordLength = 32;  
            
            FileStream readFileStream = new FileStream("../../../sound.wav", FileMode.Open, FileAccess.Read);
            BufferedReader reader = new BufferedReader(80000000, readFileStream);

            FileStream writeFileStream = new FileStream("../../../sound.fano", FileMode.Create, FileAccess.Write);
            BufferedWriter writer = new BufferedWriter(80000000, writeFileStream);

            var fano = new FanoEncoder(reader , writer);
            await fano.Encode(wordLength);

            readFileStream.Close(); 
            writeFileStream.Close();
            

            
            FileStream readFileStream2 = new FileStream("../../../sound.fano", FileMode.Open, FileAccess.Read);
            BufferedReader reader2 = new BufferedReader(80000000, readFileStream2);

            Console.WriteLine($"The decode file is {readFileStream2.Length} bytes length ({readFileStream2.Length *8} bits)");

           
            FileStream writeFileStream2 = new FileStream("../../../soundDecoded.wav", FileMode.Create, FileAccess.Write);
            BufferedWriter writer2 = new BufferedWriter(80000000, writeFileStream2);

            var fano2 = new FanoEncoder(reader2, writer2);
            await fano2.Decode();


            readFileStream2.Close();
            writeFileStream2.Close();
            

        }
    }
}
