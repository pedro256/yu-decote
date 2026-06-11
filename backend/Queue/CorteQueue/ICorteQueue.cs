using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using backend.Models.Corte;

namespace backend.Queue.CorteQueue
{
    public interface ICorteQueue
    {
        ValueTask EnfileirarPedidoAsync(CorteVideoPedido pedido);
        ValueTask<CorteVideoPedido> DesenfileirarPedidoAsync(CancellationToken cancellationToken);
    }
}