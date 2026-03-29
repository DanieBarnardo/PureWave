namespace PureWave.Admin.Models;

public sealed class DashboardSnapshot
{
    public int ClientCount { get; init; }
    public int IntakeCount { get; init; }
    public int ActiveProjectCount { get; init; }
    public decimal OutstandingAmount { get; init; }
    public decimal IncomeForPeriod { get; init; }
    public decimal ExpensesForPeriod { get; init; }
    public decimal NetIncomeForPeriod => IncomeForPeriod - ExpensesForPeriod;
}
