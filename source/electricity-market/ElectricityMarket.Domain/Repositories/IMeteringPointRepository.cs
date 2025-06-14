﻿// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using Energinet.DataHub.ElectricityMarket.Domain.Models;

namespace Energinet.DataHub.ElectricityMarket.Domain.Repositories;

public interface IMeteringPointRepository
{
    Task<MeteringPoint?> GetAsync(MeteringPointIdentification identification);
    Task<MeteringPoint?> GetMeteringPointForSignatureAsync(MeteringPointIdentification identification);
    Task<string> GetMeteringPointDebugViewAsync(MeteringPointIdentification identification);

    Task<IEnumerable<MeteringPointIdentification>> GetByGridAreaCodeAsync(string gridAreaCode);

    Task<IEnumerable<MeteringPoint>?> GetRelatedMeteringPointsAsync(MeteringPointIdentification identification);

    IAsyncEnumerable<MeteringPoint> GetMeteringPointsToSyncAsync(DateTimeOffset lastSyncedVersion, int batchSize = 10000);

    Task<IEnumerable<MeteringPoint>> GetChildMeteringPointsAsync(long identification);

    IAsyncEnumerable<MeteringPointHierarchy> GetCapacitySettlementMeteringPointHierarchiesToSyncAsync(DateTimeOffset lastSyncedVersion, int batchSize = 50);

    IAsyncEnumerable<MeteringPointHierarchy> GetNetConsumptionMeteringPointHierarchiesToSyncAsync(DateTimeOffset lastSyncedVersion, int batchSize = 50);

    IAsyncEnumerable<MeteringPointHierarchy> GetElectricalHeatingMeteringPointHierarchiesToSyncAsync(DateTimeOffset lastSyncedVersion, int batchSize = 50);

    IAsyncEnumerable<MeteringPointHierarchy> GetMeteringPointHierarchiesToSyncAsync(DateTimeOffset lastSyncedVersion, int batchSize = 50);
}
