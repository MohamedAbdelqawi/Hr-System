-- Create the stored procedure
CREATE PROCEDURE sp_CalculateEmployeeSalaryReport
@EmployeeId INT,
@Month INT,
@Year INT
AS
BEGIN
 DECLARE @EmployeeName NVARCHAR(MAX) = ''
DECLARE @Department NVARCHAR(MAX) = ''
DECLARE @Salary FLOAT = 0
DECLARE @AttendanceDays INT = 0
DECLARE @AbsenceDays INT = 0
DECLARE @AdditionalPerHour FLOAT = 0
DECLARE @HourlyDiscount FLOAT = 0
DECLARE @TotalDiscount FLOAT = 0
DECLARE @TotalAdditional FLOAT = 0
DECLARE @NetSalary FLOAT = 0


-- Retrieve Employee Information
SELECT @EmployeeName = CONCAT(e.FirstName, ' ', e.LastName), 
       @Department = d.DeptName,
       @Salary = e.Salary
FROM Employees e
INNER JOIN Departments d ON e.DepartmentId = d.Id
WHERE e.Id = @EmployeeId;


  -- Calculate Number of attendance days
  SELECT @AttendanceDays = COUNT(*) 
  FROM Attendances a
  WHERE a.EmployeeId = @EmployeeId
    AND MONTH(a.Date) = @Month
    AND YEAR(a.Date) = @Year
    AND a.ArrivalTime IS NOT NULL
    AND a.Date NOT IN (
      SELECT Day
      FROM PublicHolidays
    )

  -- Calculate the number of days in the month
  DECLARE @DaysInMonth INT
  SET @DaysInMonth = DAY(EOMONTH(DATEFROMPARTS(@Year, @Month, 1)))

  -- Calculate the number of public holidays in the month
  DECLARE @PublicHolidaysCount INT
  SELECT @PublicHolidaysCount = COUNT(*)
  FROM PublicHolidays
  WHERE MONTH(Day) = @Month AND YEAR(Day) = @Year

-- Calculate the number of weekend days for each month
DECLARE @WeekendDaysCount INT
 
 --- Calculate the number of weekend days for each week in the month
DECLARE @GeneralSettingsId INT;
-- Determine the GeneralSettingsId based on the EmployeeId
SELECT @GeneralSettingsId = Id
FROM GeneralSettings
WHERE (@EmployeeId IS NOT NULL AND EmployeeId = @EmployeeId)
   OR ( EmployeeId IS NULL);

SELECT @WeekendDaysCount = COUNT(*)
FROM Weekend w
 WHERE w.GeneralSettingsId = @GeneralSettingsId
 
-- Subtract the count of weekend days from the total days in the month

SET @AbsenceDays = @DaysInMonth - @AttendanceDays - @PublicHolidaysCount - (@WeekendDaysCount * 4)
-- Now @WeekendDaysCount contains the total number of weekend days in the month


 
 -- Calculate Additional per hour and Hourly discount
  SELECT @AdditionalPerHour = (e.Salary / 22.0 / 8.0 /60 ) * 
    CASE
      WHEN gs.EmployeeId IS NOT NULL THEN gs.OvertimeHour
      ELSE (SELECT TOP 1 OvertimeHour FROM GeneralSettings WHERE EmployeeId IS NULL)
    END,
    @HourlyDiscount = (e.Salary / 22.0 / 8.0 /60) * 
    CASE
      WHEN gs.EmployeeId IS NOT NULL THEN gs.DiscountHour
      ELSE (SELECT TOP 1 DiscountHour FROM GeneralSettings WHERE EmployeeId IS NULL)
    END
  FROM Employees e
  LEFT JOIN GeneralSettings gs ON e.Id = gs.EmployeeId
  WHERE e.Id = @EmployeeId


-- Calculate Total Discount
SELECT @TotalDiscount = COALESCE(
  @HourlyDiscount * (
    SUM(
      CASE
        WHEN e.ArrivalTime < a.ArrivalTime THEN DATEDIFF(MINUTE, e.ArrivalTime, a.ArrivalTime)
        ELSE 0
      END
      +
      CASE
        WHEN e.LeaveTime > a.LeaveTime THEN DATEDIFF(MINUTE, a.LeaveTime, e.LeaveTime)
        ELSE 0
      END
    )
  ), 0
)
FROM Employees e
INNER JOIN Attendances a ON e.Id = a.EmployeeId
WHERE e.Id = @EmployeeId
  AND MONTH(a.Date) = @Month
  AND YEAR(a.Date) = @Year;

SELECT @TotalAdditional = COALESCE(
  @AdditionalPerHour * (
    SUM(
      CASE
        WHEN e.LeaveTime < a.LeaveTime THEN DATEDIFF(MINUTE, e.LeaveTime, a.LeaveTime)
        ELSE 0
      END
    )
  ), 0
)
FROM Employees e
INNER JOIN Attendances a ON e.Id = a.EmployeeId
WHERE e.Id = @EmployeeId
  AND MONTH(a.Date) = @Month
  AND YEAR(a.Date) = @Year;

    
-- Calculate Net Salary
SELECT @NetSalary = CASE
    WHEN @AttendanceDays > 0 THEN
     (e.Salary / @DaysInMonth) * @AttendanceDays - @TotalDiscount + @TotalAdditional
    ELSE
        0   
      
END
FROM Employees e
WHERE e.Id = @EmployeeId;

SELECT @NetSalary = CASE
    WHEN @AttendanceDays > 0 THEN
     (e.Salary / (@DaysInMonth - (@PublicHolidaysCount+ (@WeekendDaysCount*4)))) * @AttendanceDays   - @TotalDiscount + @TotalAdditional
    ELSE
        0   
END
FROM Employees e
WHERE e.Id = @EmployeeId;


---- Add extra pay for each week where the employee attends (7 - @WeekendDaysCount) days
--IF @AttendanceDays >= 7 - @WeekendDaysCount
--BEGIN
--    DECLARE @ExtraWeeks INT;
--    SET @ExtraWeeks = FLOOR(@AttendanceDays / (@DaysInMonth - @WeekendDaysCount));

--    IF @ExtraWeeks = 1
--    BEGIN
--        SET @NetSalary = @NetSalary + (@Salary / @DaysInMonth) * @WeekendDaysCount * @ExtraWeeks;
--    END

--    IF @ExtraWeeks = 2
--    BEGIN
--        SET @NetSalary = @NetSalary + (@Salary / @DaysInMonth) * @WeekendDaysCount * @ExtraWeeks;
--    END

--    IF @ExtraWeeks >= 3 AND @AttendanceDays % (7 - @WeekendDaysCount) < (7 - @WeekendDaysCount)
--    BEGIN
--        SET @NetSalary = @NetSalary + (@Salary / @DaysInMonth) * @WeekendDaysCount * 2;
--    END
--    ELSE 
--    BEGIN
--        SET @NetSalary = @NetSalary + (@Salary / @DaysInMonth) * @WeekendDaysCount * @ExtraWeeks;
--    END

--    -- Add weekend days only if attendance in week 4 is greater than (7 - @WeekendDaysCount)
--    IF @ExtraWeeks >= 4 AND @AttendanceDays % (7 - @WeekendDaysCount) < (7 - @WeekendDaysCount)
--    BEGIN
--        SET @NetSalary = @NetSalary + (@Salary / @DaysInMonth) * @WeekendDaysCount * 3;
--    END
--    ELSE
--    BEGIN
--        SET @NetSalary = @NetSalary + (@Salary / @DaysInMonth) * @WeekendDaysCount * @ExtraWeeks;
--    END
--END

 -- Return the calculated values
SELECT 
    @EmployeeName as EmployeeName,
    @Department as Department,
    @Salary as  Salary,
    COALESCE(@AttendanceDays, 0) AS AttendanceDays,
    COALESCE(@AbsenceDays, 0) AS AbsenceDays,
    COALESCE(CAST(@AdditionalPerHour * 60 AS DECIMAL(10, 2)), 0) AS AdditionalPerHour,
    COALESCE(CAST(@HourlyDiscount * 60 AS DECIMAL(10, 2)), 0) AS HourlyDiscount,
    COALESCE(CAST(@TotalDiscount AS DECIMAL(10, 2)), 0) AS TotalDiscount,
    COALESCE(CAST(@TotalAdditional AS DECIMAL(10, 2)), 0) AS TotalAdditional,
    COALESCE(CAST(@NetSalary AS DECIMAL(10, 2)), 0) AS NetSalary,
    0 AS DefaultForNullValues; -- Default value if all preceding variables are NULL
	
end

-- Replace @EmployeeId, @Month, and @Year with the desired values
EXEC sp_CalculateEmployeeSalaryReport @EmployeeId =1015, @Month =11, @Year = 2023;

DROP PROCEDURE dbo.sp_CalculateEmployeeSalaryReport;
