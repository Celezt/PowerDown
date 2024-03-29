using Cysharp.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public interface IDetonationAsync : IDetonation
{
    public UniTask UpdateAsync(IDetonator handler, CancellationToken cancellationToken);
}
