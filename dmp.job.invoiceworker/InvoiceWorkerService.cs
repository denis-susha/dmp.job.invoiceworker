using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using DMP.BL.Constants;
using DMP.BL.Models;
using DMP.BL.Models.Configurations;
using DMP.BL.Services;
using DMP.DataAccess;
using DMP.DataAccess.Models;
using DMP.DataAccess.Models.Enumerations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Job.InvoiceWorker;

/// <summary>
/// Polls the <c>InvoiceWorkerTask</c> table and opens one Bitcart invoice WebSocket per pending invoice.
/// Every status message is forwarded to <see cref="IPaymentService"/>; once an invoice reaches a final state
/// the invoice task is replaced by a <c>TransactionWorkerTask</c> for the transaction worker.
/// </summary>
public sealed class InvoiceWorkerService(
    ILogger<InvoiceWorkerService> logger,
    IDbContextFactory<DmpDbContext> dmpContextFactory,
    IPaymentService paymentService,
    IOptions<BitcartOptions> bitcartOptions) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan CloseTimeout = TimeSpan.FromSeconds(5);
    private const int ReceiveBufferSize = 4 * 1024;

    private readonly BitcartOptions _bitcartOptions = bitcartOptions.Value;

    // InvoiceId -> OrderId of invoices that currently have an active WebSocket listener.
    private readonly ConcurrentDictionary<string, int> _invoicesInProgress = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Invoice worker is starting");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var invoices = await GetInvoicesAsync(stoppingToken);

                foreach (var invoice in invoices ?? [])
                {
                    if (!_invoicesInProgress.TryAdd(invoice.InvoiceId, invoice.OrderId))
                    {
                        continue;
                    }

                    logger.LogInformation("New invoice found: {InvoiceId}. Status: {Status}", invoice.InvoiceId, invoice.Status);

                    _ = ListenInvoiceAsync(invoice, stoppingToken);
                }

                await Task.Delay(PollInterval, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred while checking for tasks");
            }
        }

        logger.LogInformation("Invoice worker is stopping");
    }

    /// <summary>
    /// Returns all New and InProgress invoice tasks, marking the New ones as InProgress.
    /// Returns <c>null</c> if the database could not be queried.
    /// </summary>
    private async Task<List<InvoiceWorkerTaskDAL>?> GetInvoicesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var context = await dmpContextFactory.CreateDbContextAsync(cancellationToken);
            var invoiceTasks = await context.InvoiceWorkerTasks
                .Where(t => t.Status == InvoiceWorkerTaskStatus.New || t.Status == InvoiceWorkerTaskStatus.InProgress)
                .ToListAsync(cancellationToken);

            var newInvoices = invoiceTasks.Where(i => i.Status == InvoiceWorkerTaskStatus.New).ToList();
            if (newInvoices.Count > 0)
            {
                foreach (var newInvoice in newInvoices)
                {
                    newInvoice.Status = InvoiceWorkerTaskStatus.InProgress;
                }

                await context.SaveChangesAsync(cancellationToken);
            }

            return invoiceTasks;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while querying the database");
            return null;
        }
    }

    private async Task ListenInvoiceAsync(InvoiceWorkerTaskDAL invoice, CancellationToken stoppingToken)
    {
        logger.LogInformation("Starting WebSocket listener for invoice {InvoiceId}", invoice.InvoiceId);

        using var client = new ClientWebSocket();

        try
        {
            await client.ConnectAsync(new Uri($"{_bitcartOptions.WebSocketUri}/{invoice.InvoiceId}"), stoppingToken);
            logger.LogInformation("WebSocket connected for invoice {InvoiceId}", invoice.InvoiceId);

            var buffer = new byte[ReceiveBufferSize];
            using var messageStream = new MemoryStream();

            while (!stoppingToken.IsCancellationRequested && client.State == WebSocketState.Open)
            {
                var result = await client.ReceiveAsync(new ArraySegment<byte>(buffer), stoppingToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    logger.LogInformation("WebSocket closed by server for invoice {InvoiceId}", invoice.InvoiceId);
                    break;
                }

                // A single message may arrive in several frames; accumulate until the end of the message.
                messageStream.Write(buffer, 0, result.Count);
                if (!result.EndOfMessage)
                {
                    continue;
                }

                var message = Encoding.UTF8.GetString(messageStream.GetBuffer(), 0, (int)messageStream.Length);
                messageStream.SetLength(0);

                logger.LogInformation("Received message for invoice {InvoiceId}: {Message}", invoice.InvoiceId, message);

                var paymentUpdateMessage = JsonSerializer.Deserialize<WsPaymentUpdateMessage>(message);

                try
                {
                    ArgumentNullException.ThrowIfNull(paymentUpdateMessage);
                    await HandlePaymentUpdateAsync(invoice, paymentUpdateMessage);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to process payment update for invoice {InvoiceId}, order {OrderId}",
                        invoice.InvoiceId, invoice.OrderId);
                }
            }
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            logger.LogInformation("Listener for invoice {InvoiceId} was cancelled", invoice.InvoiceId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred with the WebSocket for invoice {InvoiceId}", invoice.InvoiceId);
        }
        finally
        {
            // Release the invoice so the polling loop picks it up again (reconnects) if its task is still pending.
            _invoicesInProgress.TryRemove(invoice.InvoiceId, out _);

            await CloseQuietlyAsync(client, invoice.InvoiceId);

            logger.LogInformation("WebSocket listener for invoice {InvoiceId} has stopped", invoice.InvoiceId);
        }
    }

    // Payment processing intentionally does not observe the stopping token: once started, the order update,
    // the API notification and the task bookkeeping should run to completion rather than stop half-way.
    private async Task HandlePaymentUpdateAsync(InvoiceWorkerTaskDAL invoice, WsPaymentUpdateMessage message)
    {
        var (orderStatus, isInvoiceFinal) =
            await paymentService.ProcessPaymentUpdateMessage(message, invoice.InvoiceId, invoice.OrderId);

        var newTaskStatus = orderStatus switch
        {
            OrderStatus.New or OrderStatus.Paid or OrderStatus.PaidPartial => InvoiceWorkerTaskStatus.InProgress,
            OrderStatus.Complete or OrderStatus.Expired or OrderStatus.PaidError => InvoiceWorkerTaskStatus.Complete,
            // Bitcart flags an overpayment while the invoice is still paid/confirmed. The transaction worker can only
            // book it once the invoice is complete, so the task is handed over only when the invoice is final.
            OrderStatus.PaidOver => isInvoiceFinal ? InvoiceWorkerTaskStatus.Complete : InvoiceWorkerTaskStatus.InProgress,
            _ => throw new ArgumentOutOfRangeException(nameof(orderStatus), orderStatus,
                $"Unexpected order status '{orderStatus}', invoice '{invoice.InvoiceId}', order '{invoice.OrderId}'."),
        };

        if (invoice.Status == newTaskStatus)
        {
            return;
        }

        logger.LogInformation("New InvoiceWorkerTask status {Status}, invoice {InvoiceId}, order {OrderId}",
            newTaskStatus, invoice.InvoiceId, invoice.OrderId);

        await using var context = await dmpContextFactory.CreateDbContextAsync();

        if (newTaskStatus == InvoiceWorkerTaskStatus.Complete)
        {
            context.InvoiceWorkerTasks.Remove(new InvoiceWorkerTaskDAL { InvoiceId = invoice.InvoiceId });

            context.TransactionWorkerTasks.Add(new TransactionWorkerTaskDAL
            {
                InvoiceId = invoice.InvoiceId,
                Status = TransactionWorkerTaskStatus.New,
                OrderId = invoice.OrderId,
                Type = TransactionWorkerTaskType.Payment,
                UserId = BlConstants.SystemUserId,
            });
        }
        else
        {
            var invoiceTask = await context.InvoiceWorkerTasks.FirstAsync(t => t.InvoiceId == invoice.InvoiceId);
            invoiceTask.Status = newTaskStatus;
        }

        await context.SaveChangesAsync();
    }

    private async Task CloseQuietlyAsync(ClientWebSocket client, string invoiceId)
    {
        if (client.State != WebSocketState.Open)
        {
            return;
        }

        try
        {
            using var timeout = new CancellationTokenSource(CloseTimeout);
            await client.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", timeout.Token);
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Failed to close WebSocket for invoice {InvoiceId}", invoiceId);
        }
    }
}
