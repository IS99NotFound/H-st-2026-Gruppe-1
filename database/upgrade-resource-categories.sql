-- Removes categories that are not registrable resources.
-- Safe to run repeatedly on an existing MariaDB volume.

DELETE FROM Category
WHERE LOWER(CategoryName) IN (
    'droneobservasjon',
    'drone observation',
    'evakuering',
    'evacuation'
)
AND NOT EXISTS (
    SELECT 1
    FROM Resource
    WHERE Resource.CategoryID = Category.CategoryID
)
AND NOT EXISTS (
    SELECT 1
    FROM ResourceNeed
    WHERE ResourceNeed.CategoryID = Category.CategoryID
);
