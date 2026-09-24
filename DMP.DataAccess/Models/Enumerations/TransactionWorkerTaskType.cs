namespace DMP.DataAccess.Models.Enumerations;

public enum TransactionWorkerTaskType : byte
{
    Payment = 0,
    Payout = 1,
    IncomingTransfer = 2,
    Bonus = 3,
}
