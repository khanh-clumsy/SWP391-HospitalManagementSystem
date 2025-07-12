public class InvoiceDetailDisplayItem
{
    public string? TestName { get; set; }
    public string? RoomName { get; set; }
    public string? Status { get; set; }
    public DateTime? CompletedAt { get; set; }
    public decimal Amount { get; set; }

    public override string ToString()
    {
        return $"Test: {TestName ?? "N/A"}, Room: {RoomName ?? "N/A"}, " +
            $"Status: {Status ?? "N/A"}, Time: {CompletedAt?.ToString("dd/MM/yyyy HH:mm") ?? "N/A"}, " +
            $"Amount: {Amount:#,##0}₫";
    }
}