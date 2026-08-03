using SQLite;

namespace SplitTrailers.Models
{
    class xLote
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
        public string fecha_captura { get; set; }
        public string tipo_captura { get; set; }
    }
}