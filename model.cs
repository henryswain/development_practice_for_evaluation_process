using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using System.ComponentModel.DataAnnotations.Schema;

[Table("transactions")]
public class Transaction {
    [JsonPropertyName("transactionId")]
    public int TransactionID { get; set; }

    [JsonPropertyName("cardNumber")]
    public string? CardNumber { get; set; }

    [JsonPropertyName("locationCode")]
    public string? LocationCode { get; set; }

    [JsonPropertyName("productName")]
    public string? ProductName { get; set; }
    
    [JsonPropertyName("amount")]
    public decimal amount { get; set; }

    [JsonPropertyName("timestamp")]
    [Column("TransactionTime")]
    public string? TimeStamp { get; set; }

    public string? Status { get; set; }
}

public class TransactionsDbContext : DbContext
{
    public DbSet<Transaction> Transaction { get; set; } = null!;

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseSqlite("Data Source=practice_development_db.db;Cache=Shared"); // SQLite database file with shared cache
    }
}



