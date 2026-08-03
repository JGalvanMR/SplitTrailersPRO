using SQLite;

namespace SplitTrailers.Models
{
    class Mensajes
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public string titulo { get; set; }
        public string mensaje { get; set; }
    }
}