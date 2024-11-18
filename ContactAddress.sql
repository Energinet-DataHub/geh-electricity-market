CREATE TABLE [dbo].[ContactAddress] (
  [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
  [StreetName] VARCHAR(100),
  [StreetCode] VARCHAR(10),
  [BuildingNumber] VARCHAR(10),
  [CityName] VARCHAR(50),
  [CitySubDivisionName] VARCHAR(50),
  [DAReference] VARCHAR(50),
  [IsProtectedAddress] BIT,
  [CountryCode] VARCHAR(10),
  [Floor] VARCHAR(10),
  [Room] VARCHAR(10),
  [PostCode] VARCHAR(10),
  [PostBox] VARCHAR(10),
  [MunicipalityCode] VARCHAR(10)
);
