-- Создание базы данных для сервисного центра
-- PostgreSQL Database Setup Script

-- Создание базы данных (выполнить от имени postgres)
CREATE DATABASE "ServiceCenterDB"
    WITH 
    OWNER = postgres
    ENCODING = 'UTF8'
    LC_COLLATE = 'Russian_Russia.1251'
    LC_CTYPE = 'Russian_Russia.1251'
    TABLESPACE = pg_default
    CONNECTION LIMIT = -1;

-- Подключиться к базе данных ServiceCenterDB и выполнить следующее:

-- Таблица клиентов
CREATE TABLE IF NOT EXISTS "Customers" (
    "Id" SERIAL PRIMARY KEY,
    "FullName" VARCHAR(200) NOT NULL,
    "Phone" VARCHAR(20) NOT NULL,
    "Email" VARCHAR(200),
    "RegisteredAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Таблица техников
CREATE TABLE IF NOT EXISTS "Technicians" (
    "Id" SERIAL PRIMARY KEY,
    "FullName" VARCHAR(200) NOT NULL,
    "Phone" VARCHAR(20) NOT NULL,
    "Specialization" VARCHAR(200) NOT NULL,
    "IsActive" BOOLEAN NOT NULL DEFAULT TRUE
);

-- Таблица заявок на ремонт
CREATE TABLE IF NOT EXISTS "ServiceRequests" (
    "Id" SERIAL PRIMARY KEY,
    "CustomerId" INTEGER NOT NULL,
    "DeviceType" VARCHAR(100) NOT NULL,
    "DeviceBrand" VARCHAR(100) NOT NULL,
    "DeviceModel" VARCHAR(100) NOT NULL,
    "SerialNumber" VARCHAR(100),
    "ProblemDescription" VARCHAR(1000) NOT NULL,
    "Status" VARCHAR(50) NOT NULL DEFAULT 'Новая',
    "EstimatedCost" DECIMAL(18,2),
    "FinalCost" DECIMAL(18,2),
    "CreatedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "CompletedAt" TIMESTAMP,
    "AssignedTechnicianId" INTEGER,
    CONSTRAINT "FK_ServiceRequests_Customers" FOREIGN KEY ("CustomerId") 
        REFERENCES "Customers"("Id") ON DELETE CASCADE,
    CONSTRAINT "FK_ServiceRequests_Technicians" FOREIGN KEY ("AssignedTechnicianId") 
        REFERENCES "Technicians"("Id") ON DELETE SET NULL
);

-- Таблица журнала работ
CREATE TABLE IF NOT EXISTS "WorkLogs" (
    "Id" SERIAL PRIMARY KEY,
    "ServiceRequestId" INTEGER NOT NULL,
    "Description" VARCHAR(1000) NOT NULL,
    "LoggedAt" TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    "LoggedBy" VARCHAR(200) NOT NULL,
    CONSTRAINT "FK_WorkLogs_ServiceRequests" FOREIGN KEY ("ServiceRequestId") 
        REFERENCES "ServiceRequests"("Id") ON DELETE CASCADE
);

-- Создание индексов для улучшения производительности
CREATE INDEX IF NOT EXISTS "IX_ServiceRequests_CustomerId" ON "ServiceRequests"("CustomerId");
CREATE INDEX IF NOT EXISTS "IX_ServiceRequests_AssignedTechnicianId" ON "ServiceRequests"("AssignedTechnicianId");
CREATE INDEX IF NOT EXISTS "IX_ServiceRequests_Status" ON "ServiceRequests"("Status");
CREATE INDEX IF NOT EXISTS "IX_ServiceRequests_CreatedAt" ON "ServiceRequests"("CreatedAt");
CREATE INDEX IF NOT EXISTS "IX_WorkLogs_ServiceRequestId" ON "WorkLogs"("ServiceRequestId");

-- Вставка тестовых данных

-- Тестовые клиенты
INSERT INTO "Customers" ("FullName", "Phone", "Email") VALUES
('Иванов Иван Иванович', '+7 (999) 123-45-67', 'ivanov@email.com'),
('Петрова Мария Сергеевна', '+7 (999) 234-56-78', 'petrova@email.com'),
('Сидоров Алексей Петрович', '+7 (999) 345-67-89', 'sidorov@email.com');

-- Тестовые техники
INSERT INTO "Technicians" ("FullName", "Phone", "Specialization", "IsActive") VALUES
('Козлов Дмитрий Владимирович', '+7 (999) 456-78-90', 'Ремонт смартфонов', TRUE),
('Смирнов Андрей Николаевич', '+7 (999) 567-89-01', 'Ремонт ноутбуков', TRUE),
('Васильев Сергей Иванович', '+7 (999) 678-90-12', 'Ремонт планшетов', TRUE);

-- Тестовые заявки
INSERT INTO "ServiceRequests" 
    ("CustomerId", "DeviceType", "DeviceBrand", "DeviceModel", "SerialNumber", 
     "ProblemDescription", "Status", "EstimatedCost", "AssignedTechnicianId") 
VALUES
(1, 'Смартфон', 'Samsung', 'Galaxy S21', 'SN123456789', 
 'Не включается, не заряжается. Возможно проблема с батареей.', 'Новая', 3500.00, 1),
(2, 'Ноутбук', 'ASUS', 'VivoBook 15', 'SN987654321', 
 'Перегревается и выключается. Требуется чистка системы охлаждения.', 'В работе', 2000.00, 2),
(3, 'Планшет', 'Apple', 'iPad Pro', 'SN456789123', 
 'Разбит экран, требуется замена дисплея.', 'Завершена', 15000.00, 3);

-- Тестовые записи в журнале работ
INSERT INTO "WorkLogs" ("ServiceRequestId", "Description", "LoggedBy") VALUES
(1, 'Заявка создана', 'Система'),
(2, 'Заявка создана', 'Система'),
(2, 'Статус изменен: Новая → В работе', 'Система'),
(2, 'Начата диагностика устройства', 'Смирнов Андрей Николаевич'),
(3, 'Заявка создана', 'Система'),
(3, 'Статус изменен: Новая → В работе', 'Система'),
(3, 'Заменен дисплей, проведено тестирование', 'Васильев Сергей Иванович'),
(3, 'Статус изменен: В работе → Завершена', 'Система');

-- Обновление финальной стоимости для завершенной заявки
UPDATE "ServiceRequests" 
SET "FinalCost" = 15000.00, "CompletedAt" = CURRENT_TIMESTAMP 
WHERE "Id" = 3;

-- Проверка данных
SELECT 'Customers' as Table_Name, COUNT(*) as Count FROM "Customers"
UNION ALL
SELECT 'Technicians', COUNT(*) FROM "Technicians"
UNION ALL
SELECT 'ServiceRequests', COUNT(*) FROM "ServiceRequests"
UNION ALL
SELECT 'WorkLogs', COUNT(*) FROM "WorkLogs";

-- Готово! База данных настроена и содержит тестовые данные.
