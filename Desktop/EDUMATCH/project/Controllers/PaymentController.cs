using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json.Serialization;

namespace EduMatch.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentController : ControllerBase
{
    private readonly EduMatchDbContext _db;
    private readonly IConfiguration _config;
    private readonly ILogger<PaymentController> _logger;
    private readonly UserManager<ApplicationUser> _userManager;

    public PaymentController(EduMatchDbContext db, IConfiguration config, ILogger<PaymentController> logger, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _config = config;
        _logger = logger;
        _userManager = userManager;
    }

    [HttpPost("sepay-webhook")]
    public async Task<IActionResult> SePayWebhook([FromBody] SePayWebhookPayload payload)
    {
        // Xác thực API token từ header
        var authHeader = Request.Headers["Authorization"].ToString();
        var expectedToken = $"Apikey {_config["SePay:ApiToken"]}";
        if (authHeader != expectedToken)
        {
            _logger.LogWarning("SePay webhook: invalid token");
            return Unauthorized();
        }

        // Chỉ xử lý giao dịch tiền vào
        if (payload.TransferType != "in")
            return Ok(new { success = true });

        var content = payload.Content ?? payload.Description ?? "";

        // Kiểm tra wallet top-up (DT...)
        var orders = await _db.PaymentOrders
            .Where(o => o.Status == TransactionStatus.Pending)
            .ToListAsync();

        var order = orders.FirstOrDefault(o =>
            !string.IsNullOrEmpty(o.PaymentGatewayOrderId) &&
            content.Contains(o.PaymentGatewayOrderId, StringComparison.OrdinalIgnoreCase));

        if (order == null)
        {
            _logger.LogWarning("SePay webhook: no matching order for content '{Content}'", content);
            return Ok(new { success = true });
        }

        if (payload.TransferAmount < order.Amount)
        {
            _logger.LogWarning("SePay webhook: amount mismatch order {Id}, expected {Expected}, got {Got}",
                order.Id, order.Amount, payload.TransferAmount);
            return Ok(new { success = true });
        }

        order.Status = TransactionStatus.Completed;
        order.CompletedAt = DateTime.UtcNow;
        order.PaymentGatewayResponse = System.Text.Json.JsonSerializer.Serialize(payload);

        var wallet = await _db.Wallets.FirstOrDefaultAsync(w => w.UserId == order.UserId);
        if (wallet != null)
        {
            var transaction = new Transaction
            {
                WalletId = wallet.Id,
                Amount = order.Amount,
                Type = TransactionType.Deposit,
                Status = TransactionStatus.Completed,
                Description = $"Nạp tiền VietQR - {order.PaymentGatewayOrderId}",
                ReferenceId = order.PaymentGatewayOrderId,
                BalanceBefore = wallet.Balance,
                BalanceAfter = wallet.Balance + order.Amount
            };
            _db.Transactions.Add(transaction);
            wallet.Balance += order.Amount;
            wallet.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync();
        _logger.LogInformation("SePay webhook: top-up order {Id} completed, amount {Amount}", order.Id, order.Amount);

        return Ok(new { success = true });
    }
}

public class SePayWebhookPayload
{
    public int Id { get; set; }
    public string? Gateway { get; set; }
    public string? TransactionDate { get; set; }
    public string? AccountNumber { get; set; }
    public string? SubAccount { get; set; }
    public string? Code { get; set; }
    public string? Content { get; set; }
    public string? Description { get; set; }

    [JsonPropertyName("transferType")]
    public string? TransferType { get; set; }

    [JsonPropertyName("transferAmount")]
    public decimal TransferAmount { get; set; }

    [JsonPropertyName("referenceCode")]
    public string? ReferenceCode { get; set; }

    public decimal Accumulated { get; set; }
}
