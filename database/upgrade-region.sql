-- Run against an existing database volume before starting the application.
-- Existing rows are assigned to Agder (RegionID 8), matching the demo default.
SET @sql = IF(
    EXISTS (
        SELECT 1
        FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Incident'
          AND COLUMN_NAME = 'RegionID'
    ),
    'SELECT 1',
    'ALTER TABLE Incident ADD COLUMN RegionID INT NOT NULL DEFAULT 8'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS (
        SELECT 1
        FROM information_schema.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE()
          AND TABLE_NAME = 'Incident'
          AND CONSTRAINT_NAME = 'fk_Incident_Region'
    ),
    'SELECT 1',
    'ALTER TABLE Incident ADD CONSTRAINT fk_Incident_Region FOREIGN KEY (RegionID) REFERENCES Region (RegionID)'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS (
        SELECT 1
        FROM information_schema.COLUMNS
        WHERE TABLE_SCHEMA = DATABASE()
          AND TABLE_NAME = 'ResourceNeed'
          AND COLUMN_NAME = 'RegionID'
    ),
    'SELECT 1',
    'ALTER TABLE ResourceNeed ADD COLUMN RegionID INT NOT NULL DEFAULT 8'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;

SET @sql = IF(
    EXISTS (
        SELECT 1
        FROM information_schema.TABLE_CONSTRAINTS
        WHERE CONSTRAINT_SCHEMA = DATABASE()
          AND TABLE_NAME = 'ResourceNeed'
          AND CONSTRAINT_NAME = 'fk_ResourceNeed_Region'
    ),
    'SELECT 1',
    'ALTER TABLE ResourceNeed ADD CONSTRAINT fk_ResourceNeed_Region FOREIGN KEY (RegionID) REFERENCES Region (RegionID)'
);
PREPARE stmt FROM @sql;
EXECUTE stmt;
DEALLOCATE PREPARE stmt;
