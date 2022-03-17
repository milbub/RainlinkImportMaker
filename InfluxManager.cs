using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using InfluxDB.Client;
using InfluxDB.Client.Core.Flux.Domain;

namespace RainlinkImportMaker
{
    public static class InfluxManager
    {
        private const string Token = "2qaZ-Uf56iNNiRizePP0ichqVwoEAxjlwIr4wy1VeZTy1X7v0uGJFoIRYuvV4qz3Zz00U277yK5FOTUTK-km9A==";
        private const string Address = "http://localhost:8086";
        private const string Bucket = "mws";
        private const string Org = "vut";

        private static readonly InfluxDBClient _client;

        static InfluxManager()
        {
            _client = InfluxDBClientFactory.Create(Address, Token);
        }

        public static Dictionary<DateTime, MwDataset> QueryUnitMinMax (string Ip, DateTime Start, DateTime End, TimeSpan Interval)
        {
            string start = XmlConvert.ToString(Start, XmlDateTimeSerializationMode.Utc);
            string end = XmlConvert.ToString(End, XmlDateTimeSerializationMode.Utc);
            string interval = ((int)Interval.TotalSeconds).ToString() + "s";

            Dictionary<DateTime, MwDataset> data = new Dictionary<DateTime, MwDataset>();

            InfluxQuery(data, Ip, start, end, interval, "min");
            InfluxQuery(data, Ip, start, end, interval, "max");

            return data;
        }

        private static void InfluxQuery(Dictionary<DateTime, MwDataset> Data, string Ip, string Start, string End, string Interval, string Function)
        {
            string flux = $"from(bucket: \"{Bucket}\")\n"
                            + $" |> range(start: {Start}, stop: {End})"
                            + $" |> filter(fn: (r) => r[\"ip\"] == \"{Ip}\")"
                            + $" |> aggregateWindow(every: {Interval}, fn: {Function}, createEmpty: false)"
                            + $" |> yield(name: \"{Function}\")";

            var fluxTables = _client.GetQueryApiSync().QuerySync(flux, Org);

            fluxTables.ForEach(fluxTable =>
            {
                var fluxRecords = fluxTable.Records;
                fluxRecords.ForEach(fluxRecord =>
                {
                    if (fluxRecord.GetTimeInDateTime().HasValue)
                    {
                        MwDataset mw;

                        if (Data.ContainsKey(fluxRecord.GetTimeInDateTime().Value))
                        {
                            mw = Data[fluxRecord.GetTimeInDateTime().Value];
                            RecordSort(fluxRecord, mw, Function);
                        }
                        else
                        {
                            mw = new MwDataset(fluxRecord.GetTimeInDateTime().Value);
                            RecordSort(fluxRecord, mw, Function);
                            Data.Add(fluxRecord.GetTimeInDateTime().Value, mw);
                        }
                    }
                });
            });
        }

        private static void RecordSort(FluxRecord Record, MwDataset Mw, string Function)
        {
            if (Record.Values.TryGetValue("_field", out object field))
            {
                if (Record.Values.TryGetValue("_value", out object value))
                {
                    if (Function == "max")
                    {
                        switch ((string)field)
                        {
                            case "rx_power":
                                Mw.MaxRxPower = (double)value;
                                break;
                            case "tx_power":
                                Mw.MaxTxPower = (double)value;
                                break;
                            case "snr":
                                Mw.MaxQuality = (double)value;
                                break;
                            case "mse":
                                Mw.MaxQuality = (double)value;
                                break;
                            case "temperature":
                                Mw.MaxTemperature = (double)value;
                                break;
                            case "modulation":
                                Mw.MaxModulation = (double)value;
                                break;
                            default:
                                break;
                        }
                    }
                    else
                    {
                        switch ((string)field)
                        {
                            case "rx_power":
                                Mw.MinRxPower = (double)value;
                                break;
                            case "tx_power":
                                Mw.MinTxPower = (double)value;
                                break;
                            case "snr":
                                Mw.MinQuality = (double)value;
                                break;
                            case "mse":
                                Mw.MinQuality = (double)value;
                                break;
                            case "temperature":
                                Mw.MinTemperature = (double)value;
                                break;
                            case "modulation":
                                Mw.MinModulation = (double)value;
                                break;
                            default:
                                break;
                        }
                    }
                }
            }
        }
    }
}
