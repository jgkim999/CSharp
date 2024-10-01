using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace RadzenDemo.Models.TestDb
{
    [Table("user_account")]
    public partial class UserAccount
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        [Column("userId")]
        public long UserId { get; set; }

        [Column("name")]
        public string Name { get; set; }

        [Column("age")]
        [Required]
        public int Age { get; set; }
    }
}