-- Fix: remove WRONG unique index on (SiteId, DestId, EffectiveDate)
-- One import row creates MULTIPLE PriceRules (weight tiers + overweight) with the same EffectiveDate.
-- schema.sql only defines non-unique IX_PriceRules_Site_Dest.

USE CloudWarehouse;
GO

IF EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PriceRules_SiteId_DestId_EffectiveDate'
      AND object_id = OBJECT_ID(N'dbo.PriceRules')
)
    DROP INDEX IX_PriceRules_SiteId_DestId_EffectiveDate ON dbo.PriceRules;
GO

IF NOT EXISTS (
    SELECT 1 FROM sys.indexes
    WHERE name = N'IX_PriceRules_Site_Dest'
      AND object_id = OBJECT_ID(N'dbo.PriceRules')
)
    CREATE INDEX IX_PriceRules_Site_Dest ON dbo.PriceRules(SiteId, DestId);
GO

PRINT 'Done. Re-run price table import.';
GO
