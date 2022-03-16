using SQLite;

namespace RainlinkParser
{
    public class MwLink
    {
        [PrimaryKey]
        [Column("id")]
        public int Id { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("tech")]
        public string Tech { get; set; }

        [Column("nameA")]
        public string NameA { get; set; }

        [Column("nameB")]
        public string NameB { get; set; }

        [Column("freqA")]
        public int FreqA { get; set; }

        [Column("freqB")]
        public int FreqB { get; set; }

        [Column("polarization")]
        public string Polarization { get; set; }

        [Column("ipA")]
        public string IpA { get; set; }

        [Column("ipB")]
        public string IpB { get; set; }

        [Column("latA")]
        public double LatA { get; set; }

        [Column("longA")]
        public double LongA { get; set; }

        [Column("latB")]
        public double LatB { get; set; }

        [Column("longB")]
        public double LongB { get; set; }

        [Column("distance")]
        public double Distance { get; set; }
    }
}
