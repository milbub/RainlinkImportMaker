using System;
using System.Collections.Generic;
using Microsoft.VisualBasic.FileIO;

namespace RainlinkParser
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 3)
            {
                Console.WriteLine("Bad arguments. Please use:\nRainlinkParser.exe <path to MWLs list> <start time> <end time>");
                return;
            }

            /* MW LIST READING */

            Dictionary<int, byte> MwsToLoad = new Dictionary<int, byte>();

            Console.Write("Loading MWLs list... ");

            TextFieldParser csvParser = new TextFieldParser(args[0])
            {
                CommentTokens = new string[] { "#" },
                Delimiters = new string[] { "," },
                TrimWhiteSpace = true,
                HasFieldsEnclosedInQuotes = false
            };

            while (!csvParser.EndOfData)
            {
                int id;
                byte mwunit;

                try
                {
                    string[] fields = csvParser.ReadFields();

                    if (fields.Length != 2)
                    {
                        throw new MalformedLineException("Too much/few columns! Need two: <MW ID>, <a/b/ab>.");
                    }

                    if (!Int32.TryParse(fields[0], out id))
                    {
                        throw new MalformedLineException("MW ID is not valid integer!");
                    }

                    mwunit = (fields[1]) switch
                    {
                        "a" => 1,
                        "b" => 2,
                        "ab" => 3,
                        _ => throw new MalformedLineException("Uknown MW unit specification. Use <a/b/ab>."),
                    };
                }
                catch (MalformedLineException e)
                {
                    Console.WriteLine("\nLine {0} is not valid:\n" + e.Message, csvParser.LineNumber);
                    continue;
                }

                MwsToLoad.Add(id, mwunit);
            }

            Console.WriteLine("OK.");

            /* LOAD MWS FROM SQLITE DB */

        }
    }
}
