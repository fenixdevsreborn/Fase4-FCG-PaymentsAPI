using MassTransit;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;

namespace PaymentsApi.Consumers;

public class OrderPlacedConsumer : IConsumer<OrderPlacedEvent>
{
  private readonly ILogger<OrderPlacedConsumer> _logger;

  public OrderPlacedConsumer(ILogger<OrderPlacedConsumer> logger)
  {
    _logger = logger;
  }

  public async Task Consume(ConsumeContext<OrderPlacedEvent> context)
  {
    var message = context.Message;
    var correlationId = context.CorrelationId ?? context.ConversationId;

    _logger.LogInformation(
      "[PaymentsAPI] Pagamento recebido. OrderId: {OrderId}, GameId: {GameId}, UserId: {UserId}, Price: {Price}, CorrelationId: {CorrelationId}",
      message.OrderId,
      message.GameId,
      message.UserId,
      message.Price,
      correlationId);

    // Regra: Preço 0.00 sempre aprovado, outros valores decididos aleatoriamente
    bool approved;
    if (message.Price == 0.00m)
    {
      approved = true;
      _logger.LogInformation("[PaymentsAPI] Pagamento com preço 0.00 - Approved automaticamente. OrderId: {OrderId}", message.OrderId);
    }
    else
    {
      // Função aleatória para decidir aprovação
      approved = Random.Shared.Next(0, 2) == 1;
      _logger.LogInformation("[PaymentsAPI] Decisão aleatória: {Status}. OrderId: {OrderId}, Price: {Price}",
        approved ? "Approved" : "Rejected", message.OrderId, message.Price);
    }

    var paymentResult = new PaymentProcessedEvent(
        message.OrderId,
        message.UserId,
        message.GameId,
        approved ? "Approved" : "Rejected"
    );

    // Publicar evento - o MassTransit usará o entity name configurado
    await context.Publish(paymentResult);

    _logger.LogInformation(
      "[PaymentsAPI] Pagamento {Status} publicado. OrderId: {OrderId}, GameId: {GameId}, UserId: {UserId}, CorrelationId: {CorrelationId}",
      paymentResult.Status,
      paymentResult.OrderId,
      paymentResult.GameId,
      paymentResult.UserId,
      correlationId);
  }
}
