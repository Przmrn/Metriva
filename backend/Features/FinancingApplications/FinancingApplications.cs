namespace Metriva.Features.FinancingApplications;

public class FinancingApplications
{
    public Guid Id { get; set; } = Guid.NewGuid();

    // Link this applicaiton to an existing customer
    public Guid CustomerId { get; set; }

    public decimal PurchasePrice { get; set; }

    public decimal DownPayment { get; set; }

    public decimal AmountFinanced { get; set; }

    public int TermMonths { get; set; }

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
