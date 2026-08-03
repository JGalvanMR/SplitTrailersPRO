using SQLite;

namespace SplitTrailers.Models
{
    class xLoteFinal
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Tipo { get; set; }
        public string Pedido { get; set; }
        public string Folio { get; set; }
        public string Codigo { get; set; }
        public string Tarima { get; set; }
        public string Cajas { get; set; }
        public string nombre { get; set; }
        public string diacad { get; set; }
        public string mescad { get; set; }
    }
}