namespace Metriva.Features.FinancingApplications;

public record CreateFinancingApplicationRequest(
  Guid CustomerId,
  decimal PurchasePrice,
  decimal DownPayment,
  int TermMonths
);

