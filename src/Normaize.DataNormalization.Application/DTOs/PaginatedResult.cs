using System;
using System.Collections.Generic;

namespace Normaize.DataNormalization.Application.DTOs;

public sealed record PaginatedResult<T>(IReadOnlyList<T> Items, int TotalItems)
{
    public PaginatedResult(IEnumerable<T> items, int totalItems)
        : this(new List<T>(items ?? throw new ArgumentNullException(nameof(items))), totalItems)
    {
    }
}
