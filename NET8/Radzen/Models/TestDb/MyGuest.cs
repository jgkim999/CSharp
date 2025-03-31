using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RadzenDemo.Models.TestDb
{
    [Table("MyGuests")]
    public partial class MyGuest
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("id")]
        public uint Id { get; set; }

        [Column("firstname")]
        [Required]
        public string Firstname { get; set; }

        [Column("lastname")]
        [Required]
        public string Lastname { get; set; }

        [Column("email")]
        public string Email { get; set; }

        [Column("reg_date")]
        public DateTime? RegDate { get; set; }
    }
}