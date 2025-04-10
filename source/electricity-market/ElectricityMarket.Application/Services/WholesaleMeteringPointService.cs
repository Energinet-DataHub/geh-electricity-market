using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Energinet.DataHub.ElectricityMarket.Application.Models;
using Energinet.DataHub.ElectricityMarket.Domain.Models;

namespace Energinet.DataHub.ElectricityMarket.Application.Services
{
    public class WholesaleMeteringPointService : IWholesaleMeteringPointService
    {
        private readonly ConnectionState[] _relevantConnectionStates = [ConnectionState.Connected, ConnectionState.Disconnected];
        private readonly MeteringPointType[] _relevantMeteringPointTypes = [MeteringPointType.Consumption, MeteringPointType.Production];

        public Task<IEnumerable<WholesaleMeteringPointDto>> GetParentWholesaleMeteringPointsAsync(IAsyncEnumerable<MeteringPoint> meteringPoints)
        {
            var wholesaleMeteringPoints = meteringPoints
                .SelectMany(mp => mp.MetadataTimeline
                    .Where(mpm => mpm.Parent is null &&
                        _relevantConnectionStates.Contains(mpm.ConnectionState) &&
                        _relevantMeteringPointTypes.Contains(mpm.Type)))
        }

        public Task<IEnumerable<WholesaleMeteringPointDto>> GetChildWholesaleMeteringPointsAsync(IAsyncEnumerable<MeteringPoint> meteringPoints, IEnumerable<string> parentMeteringPointIds)
        {
            throw new NotImplementedException();
        }
    }
}
