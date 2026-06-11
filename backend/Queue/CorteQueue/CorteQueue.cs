using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Channels;
using System.Threading.Tasks;
using backend.Models.Corte;

namespace backend.Queue.CorteQueue
{
    public class CorteQueue : ICorteQueue
    {
        private readonly Channel<CorteVideoPedido> _queue = Channel.CreateUnbounded<CorteVideoPedido>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false }
        );

        public async ValueTask EnfileirarPedidoAsync(CorteVideoPedido pedido)
        {
            ArgumentNullException.ThrowIfNull(pedido);
            await _queue.Writer.WriteAsync(pedido);
        }

        public async ValueTask<CorteVideoPedido> DesenfileirarPedidoAsync(CancellationToken cancellationToken)
        {
            return await _queue.Reader.ReadAsync(cancellationToken);
        }
    }
}