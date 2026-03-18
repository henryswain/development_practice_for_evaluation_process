using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text.Json;

public class Program {
    public static void Main(string[] args) {

        // replace json string with actual API call
        string jsonString = """
            [
                {
                "transactionId": 1001,
                "cardNumber": "4111111111111111",
                "locationCode": "STO-01",
                "productName": "Wireless Mouse",
                "amount": 19.99,
                "timestamp": "2026-02-27T08:50:10Z"
                },
                {
                "transactionId": 1002,
                "cardNumber": "4000000000000002",
                "locationCode": "STO-02",
                "productName": "USB-C Cable",
                "amount": 25.0,
                "timestamp": "2026-03-18T06:15:30Z"
                }
            ]
            """;

        // initialize list to parsed json input
        List<Transaction> transactions = JsonSerializer.Deserialize<List<Transaction>>(
            jsonString, new JsonSerializerOptions 
            { PropertyNameCaseInsensitive = true }) ?? new List<Transaction>();
        using (var db = new TransactionsDbContext())
            {
                db.Database.EnsureCreated(); // create file and schema if missing
                
                // convert transactions in database to list
                var allTransactions = db.Transaction.ToList();
                
                // build lookup from the tracked entities so updates apply to tracked instances
                var trackedById = allTransactions.ToDictionary(t => t.TransactionID);

                // loop through transactions list from database
                foreach (var transaction in transactions)
                {
                    var timeFromJson = DateTimeOffset.Parse(transaction.TimeStamp ?? "").ToUniversalTime();
                    var now = DateTimeOffset.UtcNow;
                    var timeDifference = now - timeFromJson;
                    var greaterThan24h = timeDifference.TotalHours > 24;
                    Console.WriteLine(timeDifference);
                    Console.WriteLine(greaterThan24h);
                    if (trackedById.TryGetValue(transaction.TransactionID, out var tracked))
                    {
                        // update the tracked entity if the transactionID is found in database
                        tracked.CardNumber = transaction.CardNumber;
                        tracked.LocationCode = transaction.LocationCode;
                        tracked.ProductName = transaction.ProductName;
                        tracked.amount = transaction.amount;
                        tracked.TimeStamp = transaction.TimeStamp;
                        tracked.Status = "active";
                    }
                    else
                    {
                        // insert new record when transactionId is not found
                        db.Transaction.Add(new Transaction {
                            TransactionID = transaction.TransactionID,
                            CardNumber = transaction.CardNumber,
                            LocationCode = transaction.LocationCode,
                            ProductName = transaction.ProductName,
                            amount = transaction.amount,
                            TimeStamp = transaction.TimeStamp,
                            Status = "active"
                        });
                    }
                }
                // saveUpdates
                db.SaveChanges();
            }
    }
}


