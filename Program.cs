using Microsoft.VisualBasic.FileIO;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Xml;

namespace RainlinkImportMaker
{
    class Program
    {
        static void Main(string[] args)
        {
            /***** ARGS PARSING *****/

            if (args.Length != 5)
            {
                Console.WriteLine("Bad arguments. Please use:\nRainlinkImportMaker.exe <path to MWLs list> <START time in RFC 3339> <END time in RFC 3339> <interval in minutes> <path to output CSV>");
                return;
            }

            DateTime start, end;
            TimeSpan interval;

            try
            {
                start = XmlConvert.ToDateTime(args[1], XmlDateTimeSerializationMode.Utc);
                end = XmlConvert.ToDateTime(args[2], XmlDateTimeSerializationMode.Utc);
                interval = TimeSpan.FromMinutes(double.Parse(args[3], CultureInfo.InvariantCulture));
            }
            catch (FormatException f)
            {
                Console.WriteLine("Bad argument format. Check the arguments.\n" + f.Message);
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error during arguments parsing. Check the arguments.\n" + e.Message);
                return;
            }

            /***** MW LIST READING *****/

            Console.Write("Loading MWLs list... ");

            TextFieldParser csvParser;
            try
            {
                csvParser = new TextFieldParser(args[0])
                {
                    CommentTokens = new string[] { "#" },
                    Delimiters = new string[] { "," },
                    TrimWhiteSpace = true,
                    HasFieldsEnclosedInQuotes = false
                };
            }
            catch (ArgumentNullException e)
            {
                Console.WriteLine("\nPath to MWLs list in first argument is not valid:\n" + e.Message);
                return;
            }

            Dictionary<int, byte> MwsToLoad = new Dictionary<int, byte>();

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

            /***** LOAD MWS FROM SQLITE DB *****/

            Console.Write("Loading selected MWLs from database... ");

            List<MwUnit> LoadedUnits = LinksLoader.LoadSelectedLinks(MwsToLoad);

            Console.WriteLine("OK.");

            /***** LOOP: DATA QUERY -> OUTPUT WRITE *****/

            Dictionary<string, Dictionary<DateTime, MwDataset>> queriedMws = new Dictionary<string, Dictionary<DateTime, MwDataset>>();
            StreamWriter writer = new StreamWriter(args[4]);
            writer.WriteLine("Frequency,DateTime,Pmin,Pmax,PathLength,XStart,YStart,XEnd,YEnd,ID,Polarization,WAA");

            for (int i = 0; i < LoadedUnits.Count; i++)
            {
                Console.Write("\rProcessing MW unit and writing to output: {0,4} /{1,4}", i + 1, LoadedUnits.Count);
                /* QUERY INFLUXDB TIMESERIES DATA */

                Dictionary<DateTime, MwDataset> setLocal;
                Dictionary<DateTime, MwDataset> setRemote;

                if (queriedMws.ContainsKey(LoadedUnits[i].Ip))
                    setLocal = queriedMws[LoadedUnits[i].Ip];
                else
                {
                    if (LoadedUnits[i].IsTxVolatile)
                        setLocal = InfluxManager.QueryUnitMean(LoadedUnits[i].Ip, start, end, interval);
                    else
                        setLocal = InfluxManager.QueryUnitMinMax(LoadedUnits[i].Ip, start, end, interval);
                }

                /* MODIFY SIGNAL POWER DATA (local Rx power - remote Tx power) */
                // only for Tx volatile MW unit models

                if (LoadedUnits[i].IsTxVolatile)
                {
                    if (queriedMws.ContainsKey(LoadedUnits[i].IpRemote))
                        setRemote = queriedMws[LoadedUnits[i].IpRemote];
                    else
                        setRemote = InfluxManager.QueryUnitMean(LoadedUnits[i].IpRemote, start, end, interval);

                    // remove times where remote Tx power is not available
                    List<DateTime> keysToRemove = new List<DateTime>();
                    foreach (var mwset in setLocal)
                    {
                        if (!setRemote.ContainsKey(mwset.Key))
                            keysToRemove.Add(mwset.Key);
                        else if (setRemote[mwset.Key].MinTxPower == 0)
                            keysToRemove.Add(mwset.Key);
                    }
                    foreach (var key in keysToRemove)
                    {
                        setLocal.Remove(key);
                    }

                    // local Rx power - remote Tx power
                    foreach (var mwset in setLocal)
                    {
                        mwset.Value.MinRxPower -= setRemote[mwset.Key].MinTxPower;
                        mwset.Value.MaxRxPower = mwset.Value.MinRxPower;
                    }
                }

                /* WRITE TO CSV */

                double freq, xstart, ystart, xend, yend;

                if (LoadedUnits[i].Ip == LoadedUnits[i].Link.IpA) // = A
                {
                    freq = (double)LoadedUnits[i].Link.FreqA / 1000;
                    xstart = LoadedUnits[i].Link.LongA;
                    ystart = LoadedUnits[i].Link.LatA;
                    xend = LoadedUnits[i].Link.LongB;
                    yend = LoadedUnits[i].Link.LatB;
                }
                else                                              // = B
                {
                    freq = (double)LoadedUnits[i].Link.FreqB / 1000;
                    xstart = LoadedUnits[i].Link.LongB;
                    ystart = LoadedUnits[i].Link.LatB;
                    xend = LoadedUnits[i].Link.LongA;
                    yend = LoadedUnits[i].Link.LatA;
                }

                string id = LoadedUnits[i].Ip.Replace(".", string.Empty);
                string polar = LoadedUnits[i].Link.Polarization.Remove(1).ToUpper();

                foreach (var mwset in setLocal)
                {
                    string time = mwset.Key.ToUniversalTime().ToString("yyyyMMddHHmm");
                    writer.WriteLine($"{freq.ToString("0.000", CultureInfo.InvariantCulture)}," +
                        $"{time},{mwset.Value.MinRxPower.ToString("0.000", CultureInfo.InvariantCulture)},{mwset.Value.MaxRxPower.ToString("0.000", CultureInfo.InvariantCulture)}," +
                        $"{LoadedUnits[i].Link.Distance.ToString("0.000", CultureInfo.InvariantCulture)},{xstart.ToString("0.000000", CultureInfo.InvariantCulture)},{ystart.ToString("0.000000", CultureInfo.InvariantCulture)}," +
                        $"{xend.ToString("0.000000", CultureInfo.InvariantCulture)},{yend.ToString("0.000000", CultureInfo.InvariantCulture)},{id},{polar},{LoadedUnits[i].Link.WAA.ToString("0.00", CultureInfo.InvariantCulture)}");
                }
            }

            writer.Flush();
            writer.Close();
            Console.WriteLine("\nDone!");
        }
    }
}
