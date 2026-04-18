-- ============================================================
--  TechStore Database Setup
--  Run this as MySQL root before starting the API
-- ============================================================

CREATE DATABASE IF NOT EXISTS techstore_db
    CHARACTER SET utf8mb4
    COLLATE utf8mb4_unicode_ci;

CREATE USER IF NOT EXISTS 'techstore_user'@'localhost'
    IDENTIFIED BY 'YourPassword123!';

GRANT ALL PRIVILEGES ON techstore_db.* TO 'techstore_user'@'localhost';

FLUSH PRIVILEGES;

-- Entity Framework Core migrations will create all tables automatically
-- on first startup via db.Database.MigrateAsync() in Program.cs

-- To seed an admin user after first run, update the role:
-- UPDATE techstore_db.Users SET Role = 'Admin' WHERE Email = 'admin@techstore.com';
