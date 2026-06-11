using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace backend.Models.Corte;

public record CorteVideoPedido(
    string TaskId,
    string Url,
    int Inicio,
    int Fim,
    string BucketName,
    string KeyName
);
