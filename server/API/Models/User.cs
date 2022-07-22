using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

public class User
{
    [Key]
    public int Id { get; set; }
    
    public string Username { get; set; } = string.Empty;

    [Required]
    [Column(TypeName = "varchar(50)")]
    public string Email { get; set; }
    public byte[] PasswordHash { get; set; }
    public byte[] PasswordSalt { get; set; }
}