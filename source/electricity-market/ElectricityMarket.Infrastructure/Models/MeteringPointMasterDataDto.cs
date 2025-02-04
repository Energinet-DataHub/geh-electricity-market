using System;
using System.Collections.Generic;

namespace Energinet.DataHub.ElectricityMarket.Infrastructure.Models;

public sealed record MeteringPointMasterDataDto(
    string Identification,
    DateTimeOffset ValidFrom,
    DateTimeOffset ValidTo,
    string GridAreaCode,
    string GridAccessProvider,
    IReadOnlyCollection<string> NeighborGridAreaOwners,
    ConnectionState ConnectionState);
