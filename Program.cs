using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;

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
                "timestamp": "2026-02-27T06:15:30Z"
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

                // loop through transactions list from JSON input
                foreach (var transaction in transactions) {
                    if (trackedById.TryGetValue(transaction.TransactionID, out var tracked))
                    {
                        // update the tracked entity if the transactionID is found in database
                        // and it hasn't been finalized yet
                        if (!string.Equals(tracked.Status, "finalized", StringComparison.OrdinalIgnoreCase))
                        {
                            tracked.CardNumber = transaction.CardNumber;
                            tracked.LocationCode = transaction.LocationCode;
                            tracked.ProductName = transaction.ProductName;
                            tracked.amount = transaction.amount;
                            tracked.TimeStamp = transaction.TimeStamp;
                        }
                    }
                    else
                    {
                        // insert new record when transactionId is not found
                        var added = new Transaction {
                            TransactionID = transaction.TransactionID,
                            CardNumber = transaction.CardNumber,
                            LocationCode = transaction.LocationCode,
                            ProductName = transaction.ProductName,
                            amount = transaction.amount,
                            TimeStamp = transaction.TimeStamp,
                            Status = "active"
                        };

                        db.Transaction.Add(added);
                        // keep the lookup in sync so we don't try to add the same key again
                        trackedById[added.TransactionID] = added;
                    }
                }

                // reinitialize transactions db list
                allTransactions = db.Transaction.ToList();

                var jsonById = transactions.ToDictionary(t => t.TransactionID);
  
                foreach (var db_transaction in allTransactions) {
                    // calculate time difference for determining revocation and finalization
                    if (!DateTimeOffset.TryParse(db_transaction.TimeStamp ?? "", out var timeFromDB))
                        continue;

                    timeFromDB = timeFromDB.ToUniversalTime();
                    var now = DateTimeOffset.UtcNow;
                    var timeDifference = now - timeFromDB;
                    var lessThan24h = timeDifference.TotalHours < 24;
                    // updated status based on transaction age and presence
                    int dbId = db_transaction.TransactionID;
                    if (lessThan24h) {
                        if (!jsonById.TryGetValue(dbId, out _)) {
                            db_transaction.Status = "revoked";
                        }
                        else {
                            db_transaction.Status = "active";
                        }
                    }
                    else {
                        db_transaction.Status = "finalized";
                    }
                }
    
                // saveUpdates
                db.SaveChanges();
            }
        }
    }



