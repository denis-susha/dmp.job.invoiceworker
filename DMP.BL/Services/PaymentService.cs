using System.Globalization;
using System.Text;
using System.Text.Json;
using DMP.BL.Constants;
using DMP.BL.Models;
using DMP.BL.Models.Bitcart;
using DMP.BL.Models.Configurations;
using DMP.BL.Models.Enumerations;
using DMP.BL.Models.Mail;
using DMP.DataAccess;
using DMP.DataAccess.Models;
using DMP.DataAccess.Models.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DMP.BL.Services;

public sealed class PaymentService(
    ILogger<PaymentService> logger,
    IOptions<DmpHostsSettings> dmpHostsOptions,
    IDbContextFactory<DmpDbContext> dmpContextFactory,
    IHttpClientFactory httpClientFactory,
    IOptions<MinioSettings> minioOptions) : IPaymentService
{
    private const string PaymentUpdatePath = "messages/workernotification/paymentupdate";
    private const string PurchaseCompleteTemplateName = "ClientPurchaseComplete";

    private static readonly JsonSerializerOptions ApiJsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly DmpHostsSettings _dmpHostsSettings = dmpHostsOptions.Value;
    private readonly MinioSettings _minioSettings = minioOptions.Value;

    public async Task<PaymentUpdateResult> ProcessPaymentUpdateMessage(WsPaymentUpdateMessage message, string invoiceId, int orderId)
    {
        ArgumentNullException.ThrowIfNull(message);

        var payment = new PaymentDAL
        {
            OrderId = orderId,
            // Bitcart sends amounts as invariant-culture decimal strings ("0.00012").
            Amount = decimal.Parse(message.SentAmount, CultureInfo.InvariantCulture),
            CurrencyCode = message.PaidCurrency,
        };

        var invoiceStatus = Enum.Parse<InvoiceStatusBC>(message.Status, ignoreCase: true);
        var invoiceExceptionStatus = Enum.Parse<InvoiceExceptionStatusBC>(message.ExceptionStatus, ignoreCase: true);

        var newOrderStatus = ToOrderStatus(invoiceStatus, invoiceExceptionStatus);
        var result = new PaymentUpdateResult(
            newOrderStatus,
            IsInvoiceFinal: invoiceStatus is InvoiceStatusBC.Complete or InvoiceStatusBC.Expired or InvoiceStatusBC.Invalid);

        Guid userId;
        await using (var context = await dmpContextFactory.CreateDbContextAsync())
        {
            var orderHeader = await context.OrderHeaders.FirstOrDefaultAsync(o =>
                    o.OrderId == orderId && o.ReferenceNumber == invoiceId)
                ?? throw new InvalidOperationException($"Order '{orderId}' with ReferenceNumber '{invoiceId}' not found.");

            orderHeader.Status = newOrderStatus;
            context.Payments.Add(payment);

            await context.SaveChangesAsync();

            userId = orderHeader.UserId;
        }

        var newPaymentStatus = ToPaymentStatus(newOrderStatus);

        var request = new PaymentUpdateRequest
        {
            UserId = userId,
            Status = newPaymentStatus,
            SentAmount = payment.Amount,
            OrderId = orderId,
        };

        var client = httpClientFactory.CreateClient(DmpWebApiOptions.Position);
        using var content = new StringContent(
            JsonSerializer.Serialize(request, ApiJsonSerializerOptions), Encoding.UTF8, "application/json");
        using var response = await client.PostAsync(PaymentUpdatePath, content);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogError("DMP.API.WEB returned unsuccessful status code {StatusCode} for order {OrderId}",
                (int)response.StatusCode, orderId);
            return result;
        }

        var responseContent = await response.Content.ReadAsStringAsync();
        logger.LogInformation("DMP.API.WEB returned message: {Response}", responseContent);

        if (newPaymentStatus == PaymentStatus.Complete)
        {
            await QueuePurchaseCompleteMailAsync(userId, orderId);
        }

        return result;
    }

    private static OrderStatus ToOrderStatus(InvoiceStatusBC invoiceStatus, InvoiceExceptionStatusBC exceptionStatus)
    {
        // An expired invoice is final regardless of a partial payment.
        if (exceptionStatus == InvoiceExceptionStatusBC.NONE || invoiceStatus == InvoiceStatusBC.Expired)
        {
            return invoiceStatus switch
            {
                InvoiceStatusBC.Unconfirmed or InvoiceStatusBC.Confirmed or InvoiceStatusBC.Pending => OrderStatus.New,
                InvoiceStatusBC.Paid => OrderStatus.Paid,
                InvoiceStatusBC.Expired => OrderStatus.Expired,
                InvoiceStatusBC.Invalid => OrderStatus.PaidError,
                InvoiceStatusBC.Complete => OrderStatus.Complete,
                InvoiceStatusBC.Refunded => OrderStatus.Refunded,
                _ => throw new ArgumentOutOfRangeException(nameof(invoiceStatus), invoiceStatus,
                    $"Unexpected InvoiceStatusBC: '{invoiceStatus}'."),
            };
        }

        return exceptionStatus switch
        {
            InvoiceExceptionStatusBC.PAID_PARTIAL => OrderStatus.PaidPartial,
            InvoiceExceptionStatusBC.PAID_OVER => OrderStatus.PaidOver,
            _ => throw new ArgumentOutOfRangeException(nameof(exceptionStatus), exceptionStatus,
                $"Unexpected InvoiceExceptionStatusBC: '{exceptionStatus}'."),
        };
    }

    private static PaymentStatus ToPaymentStatus(OrderStatus orderStatus) => orderStatus switch
    {
        OrderStatus.New or OrderStatus.Paid => PaymentStatus.InProgress,
        OrderStatus.PaidOver or OrderStatus.Complete => PaymentStatus.Complete,
        OrderStatus.PaidPartial => PaymentStatus.PaidPartial,
        OrderStatus.Expired => PaymentStatus.Expired,
        OrderStatus.Refunded => PaymentStatus.Refunded,
        OrderStatus.ErrorOnCreation or OrderStatus.PaidError => PaymentStatus.PaidError,
        _ => throw new ArgumentOutOfRangeException(nameof(orderStatus), orderStatus, null),
    };

    private async Task QueuePurchaseCompleteMailAsync(Guid userId, int orderId)
    {
        await using var context = await dmpContextFactory.CreateDbContextAsync();

        var user = await context.Users
            .Where(u => u.UserId == userId)
            .Select(u => new { u.Email, u.Language })
            .FirstAsync();

        var templateId = await context.EmailTemplates
            .Where(et => et.Name == PurchaseCompleteTemplateName && et.Language == user.Language)
            .Select(et => et.EmailTemplateId)
            .FirstAsync();

        var model = new ClientPurchaseCompleteMail
        {
            Url = $"{_dmpHostsSettings.Client}/my/orderdetails?order={orderId}",
            Year = DateTime.UtcNow.Year.ToString(CultureInfo.InvariantCulture),
            LogoUrl = $"{_minioSettings.S3PublicEndpoint}/{BlConstants.LogoPath}",
        };

        context.Mails.Add(new MailDAL
        {
            To = user.Email,
            From = BlConstants.StoreEmail,
            Status = MailStatus.New,
            EmailTemplateId = templateId,
            Model = JsonSerializer.Serialize(model),
        });

        await context.SaveChangesAsync();
    }
}
