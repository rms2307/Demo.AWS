using Amazon.DynamoDBv2.DataModel;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Common.Model
{
    public enum StatusDoPedido
    {
        Coletado = 0,
        Pago = 1,
        Faturado = 2,
    };

    [DynamoDBTable("pedidos")]
    public class Pedido
    {
        public string Id { get; set; }

        public decimal ValorTotal { get; set; }

        public DateTime DataDeCriacao { get; set; }

        public List<Produto> Produtos { get; set; }

        public Cliente Cliente { get; set; }

        public Pagamento Pagamento { get; set; }

        public string Justificativa { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public StatusDoPedido Status { get; set; }

        public bool Cancelado { get; set; }
    }
}
