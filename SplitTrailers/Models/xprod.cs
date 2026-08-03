using SQLite;

namespace SplitTrailers.Models
{
    class xprod
    {

        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string Tipo { get; set; }
        public string Folio { get; set; }
        public string Codigo { get; set; }
        public string Tarima { get; set; }
        public string Cajas { get; set; }
        public string fecha_captura { get; set; }
        public string tipo_captura { get; set; }
        [Unique]
        public string Lecturabd { get; set; }
    }
}