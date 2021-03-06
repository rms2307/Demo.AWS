using Amazon.Lambda.Core;
using Amazon.Lambda.DynamoDBEvents;
using Amazon.DynamoDBv2.Model;
using Common;
using Common.Model;
using Amazon.DynamoDBv2;
using Amazon;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Coletor;

public class Function
{
    public async Task FunctionHandler(DynamoDBEvent dynamoEvent, ILambdaContext context)
    {

        foreach (var record in dynamoEvent.Records)
        {
            if (record.EventName == "INSERT")
            {
                var pedido = record.Dynamodb.NewImage.ToObject<Pedido>();
                pedido.Status = StatusDoPedido.Coletado;

                try
                {
                    await ProcessarValorDoPedido(pedido);
                    await AmazonUtil.EnviarParaFila(EnumFilasSQS.Pedido, pedido);
                    context.Logger.LogInformation($"Pedido coletado com sucesso: {pedido.Id}");
                }
                catch (Exception ex)
                {
                    context.Logger.LogError($"ERRO: {ex.Message}");
                    pedido.Justificativa = ex.Message;
                    pedido.Cancelado = true;
                    await AmazonUtil.EnviarParaFila(EnumFilasSNS.Falha, pedido);
                }

                await pedido.SalvarAsync();
            }
        }
    }

    private async Task ProcessarValorDoPedido(Pedido pedido)
    {
        foreach (var produto in pedido.Produtos)
        {
            var produtoDoEstoque = await ObterProdutoDoDynamoDBAsync(produto.Id);
            if (produtoDoEstoque == null) throw new InvalidOperationException($"Produto sem estoque. {produto.Id} : {produto.Nome}");

            produto.Valor = produtoDoEstoque.Valor;
            produto.Nome = produtoDoEstoque.Nome;
        }

        var valorTotal = pedido.Produtos.Sum(x => x.Valor * x.Quantidade);
        if (pedido.ValorTotal != 0 && pedido.ValorTotal != valorTotal)
            throw new InvalidOperationException($"O valor esperado do pedido ? de R$ {pedido.ValorTotal} e o valor verdadeiro ? R$ {valorTotal}");

        pedido.ValorTotal = valorTotal;
    }

    private async Task<Produto> ObterProdutoDoDynamoDBAsync(string id)
    {
        var client = new AmazonDynamoDBClient(RegionEndpoint.SAEast1);
        var request = new QueryRequest
        {
            TableName = "estoque",
            KeyConditionExpression = "Id = :v_id",
            ExpressionAttributeValues = new Dictionary<string, AttributeValue> { { ":v_id", new AttributeValue { S = id } } }
        };

        var response = await client.QueryAsync(request);
        var item = response.Items.FirstOrDefault();
        if (item == null) return null;
        return item.ToObject<Produto>();
    }
}