using SQLite;

namespace SplitTrailers.Models
{
    class Pedidos
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string folio { get; set; }
        public string prod_clave { get; set; }
        public string nombre { get; set; }
        public int pedido { get; set; }
        public int surtido { get; set; }

    }
}