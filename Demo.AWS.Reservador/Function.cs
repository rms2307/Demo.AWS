using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Common;
using Common.Model;
using System.Text.Json;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Demo.AWS.Reservador;

public class Function
{
    private readonly AmazonDynamoDBClient _amazonDynamo;

    public Function()
    {

    }

    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        if (evnt.Records.Count > 1)
            throw new InvalidOperationException("Somente uma mensagem pode ser tratada por vez");

        var message = evnt.Records.FirstOrDefault();
        if (message == null) return;

        await ProcessMessageAsync(message, context);
    }

    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        var pedido = JsonSerializer.Deserialize<Pedido>(message.Body);
        if (pedido == null) return;
        pedido.Status = StatusDoPedido.Reservado;

        foreach (var produto in pedido.Produtos)
        {
            try
            {
                await BaixarEstoque(produto.Id, produto.Quantidade);
                produto.Reservado = true;
                context.Logger.LogInformation($"Produto baixado do estoque: {produto.Id} - {produto.Nome}");
            }
            catch (ConditionalCheckFailedException)
            {
                pedido.Justificativa = "Produto indisponivel";
                pedido.Cancelado = true;
                context.Logger.LogError($"Erro: {pedido.Justificativa}");
                break;
            }
        }

        if (pedido.Cancelado)
        {
            foreach (var produto in pedido.Produtos)
            {
                if (produto.Reservado)
                {
                    await DevolverAoEstoque(produto.Id, produto.Quantidade);
                    produto.Reservado = false;
                    context.Logger.LogInformation($"Produto devolvido ao estoque: {produto.Id} - {produto.Nome}");
                }
            }

            await AmazonUtil.EnviarParaFila(EnumFilasSNS.Falha, pedido);
            await pedido.SalvarAsync();
        }
        else
        {
            await AmazonUtil.EnviarParaFila(EnumFilasSQS.Reservado, pedido);
            await pedido.SalvarAsync();
        }
    }

    private async Task DevolverAoEstoque(string id, int quantidade)
    {
        var request = new UpdateItemRequest
        {
            TableName = "estoque",
            ReturnValues = "NONE",
            Key = new Dictionary<string, AttributeValue>
            {
                {"Id", new AttributeValue{S = id} }
            },
            UpdateExpression = "SET Quantidade = (Quantidade + :quantidade)",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                {":quantidade", new AttributeValue{N = id} }
            }
        };

        await _amazonDynamo.UpdateItemAsync(request);
    }

    private async Task BaixarEstoque(string id, int quantidade)
    {
        var request = new UpdateItemRequest
        {
            TableName = "estoque",
            ReturnValues = "NONE",
            Key = new Dictionary<string, AttributeValue>
            {
                {"Id", new AttributeValue{S = id} }
            },
            UpdateExpression = "SET Quantidade = (Quantidade - :quantidade)",
            ConditionExpression = "Quantidade >= :quantidade",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue>
            {
                {":quantidade", new AttributeValue{N = id} }
            }
        };

        await _amazonDynamo.UpdateItemAsync(request);
    }
}