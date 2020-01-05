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
            byte wordLength = 3;  
            
            FileStream readFileStream = new FileStream("../../../ex2.txt", FileMode.Open, FileAccess.Read);
            BufferedReader reader = new BufferedReader(80, readFileStream);

            FileStream writeFileStream = new FileStream("../../../ex2.fano", FileMode.Create, FileAccess.Write);
            BufferedWriter writer = new BufferedWriter(80, writeFileStream);

            var fano = new FanoEncoder(reader , writer);
            await fano.Encode(wordLength);

            readFileStream.Close(); 
            writeFileStream.Close();
            

            
            FileStream readFileStream2 = new FileStream("../../../ex2.fano", FileMode.Open, FileAccess.Read);
            BufferedReader reader2 = new BufferedReader(80, readFileStream2);

            Console.WriteLine($"The decoded file is {readFileStream2.Length} bytes long ({readFileStream2.Length *8} bits)");

           
            FileStream writeFileStream2 = new FileStream("../../../ex2Decoded.txt", FileMode.Create, FileAccess.Write);
            BufferedWriter writer2 = new BufferedWriter(80, writeFileStream2);

            var fano2 = new FanoEncoder(reader2, writer2);
            await fano2.Decode();


            readFileStream2.Close();
            writeFileStream2.Close();
            

        }
    }
}
