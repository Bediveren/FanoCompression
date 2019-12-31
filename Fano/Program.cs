using IO;
using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;

namespace FanoCompression
{
    class Program
    {

        static void Main(string[] args)
        {

            /*
            ifStream = new FileStream("output.lz77", FileMode.Open, FileAccess.Read);
            reader = new BufferedReader(60000, ifStream);
            ofStream = new FileStream("output.zip", FileMode.Create, FileAccess.Write);
            writer = new BufferedWriter(60000, ofStream);
            var extr = new Extractor(reader.ReadCustomLength, writer.WriteCustomLength);
            await extr.Extract();
            await writer.FlushBuffer();
            ifStream.Close();
            ofStream.Close();
            */


            int wordLength = 8;
                
            FileStream readFileStream = new FileStream("../../../sample.txt", FileMode.Open, FileAccess.Read);
            BufferedReader reader = new BufferedReader(8000, readFileStream);

            FileStream writeFileStream = new FileStream("../../../sample.fano", FileMode.Create, FileAccess.Write);
            BufferedWriter writer = new BufferedWriter(8000, writeFileStream);

            var fano = new FanoEncoder(reader , writer, wordLength);
            // fano.Encode("../../../sample.txt", 8, "./");
            fano.Encode();
            var a = (10, 10);
            readFileStream.Close();
            writeFileStream.Close();
            //fano.decode("file.ex", "locationtosave";

        }
    }
}
