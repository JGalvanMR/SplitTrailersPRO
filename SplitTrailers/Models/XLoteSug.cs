using SQLite;


namespace SplitTrailers.Models
{
    class XLoteSug
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string recibosug { get; set; }
        public string fecrecsug { get; set; }
        public string cveprod { get; set; }
        public string Tarima { get; set; }
        public int Cajasdis { get; set; }
        public int Cajasusadas { get; set; }
        public string foliomens { get; set; }

    }
}