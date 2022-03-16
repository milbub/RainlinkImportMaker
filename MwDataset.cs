using System;

namespace RainlinkParser
{
    public class MwDataset
    {
        public DateTime Timestamp { get; set; }
        public double MinRxPower { get; set; }
        public double MinTxPower { get; set; }
        public double MinQuality { get; set; }
        public double MinTemperature { get; set; }
        public double MinModulation { get; set; }

        public double MaxRxPower { get; set; }
        public double MaxTxPower { get; set; }
        public double MaxQuality { get; set; }
        public double MaxTemperature { get; set; }
        public double MaxModulation { get; set; }

        public MwDataset(DateTime Timestamp)
        {
            this.Timestamp = Timestamp;
        }
    }
}
