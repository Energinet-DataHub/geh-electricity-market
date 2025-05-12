// Copyright 2020 Energinet DataHub A/S
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

namespace Energinet.DataHub.ElectricityMarket.Application.Common;

public static class TransactionTypes
{
    public const string ChangeSupplier = "CHANGESUP";
    public const string EndSupply = "ENDSUPPLY";
    public const string IncorrectSupplierChange = "INCCHGSUP";
    public const string MasterDataSent = "MSTDATSBM";
    public const string AttachChild = "LNKCHLDMP";
    public const string DettachChild = "ULNKCHLDMP";
    public const string MoveIn = "MOVEINES";
    public const string MoveOut = "MOVEOUTES";
    public const string TransactionTypeIncMove = "INCMOVEAUT";
    public const string IncorrectMoveIn = "INCMOVEMAN";
    public const string ElectricalHeatingOn = "MDCNSEHON";
    public const string ElectricalHeatingOff = "MDCNSEHOFF";
    public const string ChangeSupplierShort = "CHGSUPSHRT";
    public const string ManualChangeSupplier = "MANCHGSUP";
    public const string ManualCorrections = "MANCOR";
}
