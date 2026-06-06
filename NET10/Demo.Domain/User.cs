namespace Demo.Domain;

public class User
{
    [System.ComponentModel.DataAnnotations.Schema.Column("id")]
    public long Id { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.Column("email")]
    public string Email { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.Column("password")]
    public string Password { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.Column("salt")]
    public string Salt { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.Column("created_at")]
    public DateTime CreatedAt { get; set; }

    [System.ComponentModel.DataAnnotations.Schema.Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}
