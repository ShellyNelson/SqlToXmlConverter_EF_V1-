-- Test script to create sample data for the SQL to XML converter
-- Run this script in SQL Server Management Studio or Azure Data Studio

-- Create the Employees table
IF NOT EXISTS (SELECT * FROM sysobjects WHERE name='Employees' AND xtype='U')
BEGIN
    CREATE TABLE Employees (
        Id INT PRIMARY KEY IDENTITY(1,1),
        FirstName NVARCHAR(50) NOT NULL,
        LastName NVARCHAR(50) NOT NULL,
        Email NVARCHAR(100) UNIQUE NOT NULL,
        Department NVARCHAR(50),
        HireDate DATETIME2 NOT NULL,
        Salary DECIMAL(10,2)
    );
    PRINT 'Employees table created successfully.';
END
ELSE
BEGIN
    PRINT 'Employees table already exists.';
END

-- Clear existing data
DELETE FROM Employees;

-- Insert sample data
INSERT INTO Employees (FirstName, LastName, Email, Department, HireDate, Salary)
VALUES 
    ('John', 'Doe', 'john.doe@company.com', 'IT', '2023-01-15', 75000.00),
    ('Jane', 'Smith', 'jane.smith@company.com', 'HR', '2023-02-20', 65000.00),
    ('Bob', 'Johnson', 'bob.johnson@company.com', 'Finance', '2023-03-10', 80000.00),
    ('Alice', 'Williams', 'alice.williams@company.com', 'IT', '2023-04-05', 72000.00),
    ('Charlie', 'Brown', 'charlie.brown@company.com', 'Marketing', '2023-05-12', 68000.00);

PRINT 'Sample data inserted successfully.';

-- Verify the data
SELECT COUNT(*) as TotalEmployees FROM Employees;
SELECT * FROM Employees ORDER BY Id;
