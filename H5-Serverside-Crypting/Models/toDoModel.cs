using System.ComponentModel.DataAnnotations;

namespace H5_Serverside_Crypting.Models
{
    public class toDoModel
    {
        [Key]
        public int id { get; set; }

        public string Title { get; set; }

        public string Description { get; set; }

        public string HashValue { get; set; }

        public Guid UserId { get; set; }
    }
}
