CREATE TABLE [electricitymarket].[SyncJobs](
    [JobName]               [int]  NOT NULL,
    [Version]               [int]               NOT NULL,

    CONSTRAINT [PK_SyncJobs] PRIMARY KEY CLUSTERED ([JobName] ASC),
    ) ON [PRIMARY]
    GO