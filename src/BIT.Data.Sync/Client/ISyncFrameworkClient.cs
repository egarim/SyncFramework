﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace BIT.Data.Sync.Client
{
    public interface ISyncFrameworkClient
    {
        Task<List<Delta>> FetchAsync(string startIndex, string identity, CancellationToken cancellationToken);
        Task<PushOperationResponse> PushAsync(IEnumerable<IDelta> Deltas, CancellationToken cancellationToken);

        Task<FetchOperationResponse> FetchOperationAsync(string startIndex, string identity, CancellationToken cancellationToken);
        Task<PushOperationResponse> PushOperationAsync(IEnumerable<IDelta> Deltas, CancellationToken cancellationToken);
    }
}
