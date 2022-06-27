using Amazon;
using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.DataModel;
using Amazon.DynamoDBv2.DocumentModel;
using Amazon.DynamoDBv2.Model;
using Amazon.SQS;
using Amazon.SQS.Model;
using Common.Model;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Common
{
    public static class AmazonUtil
    {
        public static async Task SalvarAsync(this Pedido pedido)
        {
            DynamoDBContext context = CriarContext();

            await context.SaveAsync(pedido);
        }

        public static T ToObject<T>(this Dictionary<string, AttributeValue> dictionary)
        {
            DynamoDBContext context = CriarContext();

            var document = Document.FromAttributeMap(dictionary);
            return context.FromDocument<T>(document);
        }

        public static async Task EnviarParaFila<T>(EnumFilasSQS fila, T entity)
        {
            var json = JsonSerializer.Serialize(entity);
            var client = new AmazonSQSClient(RegionEndpoint.SAEast1);

            var request = new SendMessageRequest
            {
                QueueUrl = $"https://sqs.sa-east-1.amazonaws.com/653342422270/{fila}",
                MessageBody = json
            };

            await client.SendMessageAsync(request);
        }

        public static async Task EnviarParaFila<T>(EnumFilasSNS fila, T entity)
        {
            // TODO: Implementar envio para filas SNS
            await Task.CompletedTask;
        }

        private static DynamoDBContext CriarContext()
        {
            var client = new AmazonDynamoDBClient(RegionEndpoint.SAEast1);
            var context = new DynamoDBContext(client);
            return context;
        }
    }
}
