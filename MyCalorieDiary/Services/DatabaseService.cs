using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Microsoft.Data.Sqlite;
using MyCalorieDiary.Models;

namespace MyCalorieDiary.Services
{
    public class DatabaseService
    {
        private readonly string _dbPath;
        private readonly string _connectionString;

        public string DbPath => _dbPath;

        public DatabaseService()
        {
            // Save calorie_diary.db in the application directory
            _dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "calorie_diary.db");
            _connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = _dbPath,
                Mode = SqliteOpenMode.ReadWriteCreate
            }.ToString();

            InitializeDatabase();
        }

        private SqliteConnection GetConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        private void InitializeDatabase()
        {
            using var connection = GetConnection();
            connection.Open();

            var createFoodsTableCmd = connection.CreateCommand();
            createFoodsTableCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS Foods (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    Name TEXT NOT NULL,
                    ServingSize REAL NOT NULL DEFAULT 1.0,
                    ServingUnit TEXT NOT NULL DEFAULT '인분',
                    Calories REAL NOT NULL,
                    Carbohydrates REAL NOT NULL,
                    Protein REAL NOT NULL,
                    Fat REAL NOT NULL,
                    CreatedAt TEXT NOT NULL
                );";
            createFoodsTableCmd.ExecuteNonQuery();

            var createMealLogsTableCmd = connection.CreateCommand();
            createMealLogsTableCmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS MealLogs (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FoodId INTEGER,
                    FoodName TEXT NOT NULL,
                    MealDate TEXT NOT NULL,
                    MealType TEXT NOT NULL,
                    Amount REAL NOT NULL DEFAULT 1.0,
                    ServingUnit TEXT NOT NULL DEFAULT '인분',
                    TotalCalories REAL NOT NULL,
                    TotalCarbs REAL NOT NULL,
                    TotalProtein REAL NOT NULL,
                    TotalFat REAL NOT NULL,
                    FOREIGN KEY (FoodId) REFERENCES Foods(Id) ON DELETE SET NULL
                );";
            createMealLogsTableCmd.ExecuteNonQuery();

            // Check if Foods table is empty, seed initial data
            var countCmd = connection.CreateCommand();
            countCmd.CommandText = "SELECT COUNT(*) FROM Foods;";
            long count = (long)(countCmd.ExecuteScalar() ?? 0L);

            if (count == 0)
            {
                SeedDefaultFoods(connection);
            }
        }

        private void SeedDefaultFoods(SqliteConnection connection)
        {
            var defaultFoods = new (string Name, string Unit, double Calories, double Carbs, double Protein, double Fat)[]
            {
                ("쌀밥 (1공기)", "공기", 300, 65.0, 6.0, 1.0),
                ("현미밥 (1공기)", "공기", 290, 62.0, 6.5, 2.0),
                ("닭가슴살 (100g)", "팩", 130, 0.0, 26.0, 2.0),
                ("삶은 달걀 (1개)", "개", 75, 0.5, 6.5, 5.0),
                ("바나나 (1개)", "개", 105, 27.0, 1.3, 0.3),
                ("고구마 (1개, 150g)", "개", 190, 45.0, 2.0, 0.5),
                ("사과 (1개, 200g)", "개", 100, 25.0, 0.5, 0.3),
                ("아메리카노 (1잔)", "잔", 10, 1.0, 0.5, 0.0),
                ("우유 (200ml)", "컵", 130, 10.0, 6.0, 7.0),
                ("그릭 요거트 (100g)", "통", 95, 4.0, 10.0, 4.0),
                ("소고기 안심 (100g)", "인분", 190, 0.0, 26.0, 9.0),
                ("연어 구이 (100g)", "토막", 200, 0.0, 22.0, 12.0),
                ("신라면 (1봉지)", "봉지", 500, 80.0, 10.0, 15.0),
                ("식빵 (1쪽)", "쪽", 70, 13.0, 2.5, 1.0)
            };

            using var transaction = connection.BeginTransaction();
            foreach (var f in defaultFoods)
            {
                var cmd = connection.CreateCommand();
                cmd.Transaction = transaction;
                cmd.CommandText = @"
                    INSERT INTO Foods (Name, ServingSize, ServingUnit, Calories, Carbohydrates, Protein, Fat, CreatedAt)
                    VALUES (@Name, 1.0, @ServingUnit, @Calories, @Carbohydrates, @Protein, @Fat, @CreatedAt);";
                cmd.Parameters.AddWithValue("@Name", f.Name);
                cmd.Parameters.AddWithValue("@ServingUnit", f.Unit);
                cmd.Parameters.AddWithValue("@Calories", f.Calories);
                cmd.Parameters.AddWithValue("@Carbohydrates", f.Carbs);
                cmd.Parameters.AddWithValue("@Protein", f.Protein);
                cmd.Parameters.AddWithValue("@Fat", f.Fat);
                cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("o"));
                cmd.ExecuteNonQuery();
            }
            transaction.Commit();
        }

        public List<FoodItem> GetAllFoods()
        {
            var list = new List<FoodItem>();
            using var connection = GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                SELECT Id, Name, ServingSize, ServingUnit, Calories, Carbohydrates, Protein, Fat, CreatedAt
                FROM Foods
                ORDER BY Name COLLATE NOCASE ASC;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new FoodItem
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    ServingSize = reader.GetDouble(2),
                    ServingUnit = reader.GetString(3),
                    Calories = reader.GetDouble(4),
                    Carbohydrates = reader.GetDouble(5),
                    Protein = reader.GetDouble(6),
                    Fat = reader.GetDouble(7),
                    CreatedAt = DateTime.TryParse(reader.GetString(8), null, DateTimeStyles.RoundtripKind, out var dt) ? dt : DateTime.Now
                });
            }

            return list;
        }

        public int AddFood(FoodItem food)
        {
            using var connection = GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO Foods (Name, ServingSize, ServingUnit, Calories, Carbohydrates, Protein, Fat, CreatedAt)
                VALUES (@Name, @ServingSize, @ServingUnit, @Calories, @Carbohydrates, @Protein, @Fat, @CreatedAt);
                SELECT last_insert_rowid();";

            cmd.Parameters.AddWithValue("@Name", food.Name.Trim());
            cmd.Parameters.AddWithValue("@ServingSize", food.ServingSize <= 0 ? 1.0 : food.ServingSize);
            cmd.Parameters.AddWithValue("@ServingUnit", string.IsNullOrWhiteSpace(food.ServingUnit) ? "인분" : food.ServingUnit.Trim());
            cmd.Parameters.AddWithValue("@Calories", food.Calories);
            cmd.Parameters.AddWithValue("@Carbohydrates", food.Carbohydrates);
            cmd.Parameters.AddWithValue("@Protein", food.Protein);
            cmd.Parameters.AddWithValue("@Fat", food.Fat);
            cmd.Parameters.AddWithValue("@CreatedAt", DateTime.Now.ToString("o"));

            long newId = (long)(cmd.ExecuteScalar() ?? 0L);
            return (int)newId;
        }

        public bool DeleteFood(int id)
        {
            using var connection = GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM Foods WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id);

            return cmd.ExecuteNonQuery() > 0;
        }

        public List<MealRecord> GetMealRecords(DateTime? date = null, string? mealType = null)
        {
            var list = new List<MealRecord>();
            using var connection = GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            var conditions = new List<string>();

            if (date.HasValue)
            {
                string startStr = date.Value.Date.ToString("yyyy-MM-dd 00:00:00");
                string endStr = date.Value.Date.ToString("yyyy-MM-dd 23:59:59");
                conditions.Add("MealDate BETWEEN @StartDate AND @EndDate");
                cmd.Parameters.AddWithValue("@StartDate", startStr);
                cmd.Parameters.AddWithValue("@EndDate", endStr);
            }

            if (!string.IsNullOrWhiteSpace(mealType) && mealType != "전체")
            {
                conditions.Add("MealType = @MealType");
                cmd.Parameters.AddWithValue("@MealType", mealType);
            }

            string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";

            cmd.CommandText = $@"
                SELECT Id, FoodId, FoodName, MealDate, MealType, Amount, ServingUnit, TotalCalories, TotalCarbs, TotalProtein, TotalFat
                FROM MealLogs
                {whereClause}
                ORDER BY MealDate DESC, Id DESC;";

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new MealRecord
                {
                    Id = reader.GetInt32(0),
                    FoodId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    FoodName = reader.GetString(2),
                    MealDate = DateTime.TryParse(reader.GetString(3), out var dt) ? dt : DateTime.Now,
                    MealType = reader.GetString(4),
                    Amount = reader.GetDouble(5),
                    ServingUnit = reader.GetString(6),
                    TotalCalories = reader.GetDouble(7),
                    TotalCarbs = reader.GetDouble(8),
                    TotalProtein = reader.GetDouble(9),
                    TotalFat = reader.GetDouble(10)
                });
            }

            return list;
        }

        public List<MealRecord> GetMealRecordsByRange(DateTime startDate, DateTime endDate)
        {
            var list = new List<MealRecord>();
            using var connection = GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            string startStr = startDate.Date.ToString("yyyy-MM-dd 00:00:00");
            string endStr = endDate.Date.ToString("yyyy-MM-dd 23:59:59");

            cmd.CommandText = @"
                SELECT Id, FoodId, FoodName, MealDate, MealType, Amount, ServingUnit, TotalCalories, TotalCarbs, TotalProtein, TotalFat
                FROM MealLogs
                WHERE MealDate BETWEEN @StartDate AND @EndDate
                ORDER BY MealDate DESC, Id DESC;";

            cmd.Parameters.AddWithValue("@StartDate", startStr);
            cmd.Parameters.AddWithValue("@EndDate", endStr);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new MealRecord
                {
                    Id = reader.GetInt32(0),
                    FoodId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    FoodName = reader.GetString(2),
                    MealDate = DateTime.TryParse(reader.GetString(3), out var dt) ? dt : DateTime.Now,
                    MealType = reader.GetString(4),
                    Amount = reader.GetDouble(5),
                    ServingUnit = reader.GetString(6),
                    TotalCalories = reader.GetDouble(7),
                    TotalCarbs = reader.GetDouble(8),
                    TotalProtein = reader.GetDouble(9),
                    TotalFat = reader.GetDouble(10)
                });
            }

            return list;
        }

        public int AddMealRecord(MealRecord record)
        {
            using var connection = GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = @"
                INSERT INTO MealLogs (FoodId, FoodName, MealDate, MealType, Amount, ServingUnit, TotalCalories, TotalCarbs, TotalProtein, TotalFat)
                VALUES (@FoodId, @FoodName, @MealDate, @MealType, @Amount, @ServingUnit, @TotalCalories, @TotalCarbs, @TotalProtein, @TotalFat);
                SELECT last_insert_rowid();";

            cmd.Parameters.AddWithValue("@FoodId", (object?)record.FoodId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@FoodName", record.FoodName);
            cmd.Parameters.AddWithValue("@MealDate", record.MealDate.ToString("yyyy-MM-dd HH:mm:ss"));
            cmd.Parameters.AddWithValue("@MealType", record.MealType);
            cmd.Parameters.AddWithValue("@Amount", record.Amount);
            cmd.Parameters.AddWithValue("@ServingUnit", record.ServingUnit);
            cmd.Parameters.AddWithValue("@TotalCalories", record.TotalCalories);
            cmd.Parameters.AddWithValue("@TotalCarbs", record.TotalCarbs);
            cmd.Parameters.AddWithValue("@TotalProtein", record.TotalProtein);
            cmd.Parameters.AddWithValue("@TotalFat", record.TotalFat);

            long newId = (long)(cmd.ExecuteScalar() ?? 0L);
            return (int)newId;
        }

        public bool DeleteMealRecord(int id)
        {
            using var connection = GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = "DELETE FROM MealLogs WHERE Id = @Id;";
            cmd.Parameters.AddWithValue("@Id", id);

            return cmd.ExecuteNonQuery() > 0;
        }

        public DailyNutritionSummary GetDailySummary(DateTime date)
        {
            using var connection = GetConnection();
            connection.Open();

            var cmd = connection.CreateCommand();
            string startStr = date.Date.ToString("yyyy-MM-dd 00:00:00");
            string endStr = date.Date.ToString("yyyy-MM-dd 23:59:59");

            cmd.CommandText = @"
                SELECT 
                    COALESCE(SUM(TotalCalories), 0),
                    COALESCE(SUM(TotalCarbs), 0),
                    COALESCE(SUM(TotalProtein), 0),
                    COALESCE(SUM(TotalFat), 0),
                    COUNT(*)
                FROM MealLogs
                WHERE MealDate BETWEEN @StartDate AND @EndDate;";

            cmd.Parameters.AddWithValue("@StartDate", startStr);
            cmd.Parameters.AddWithValue("@EndDate", endStr);

            using var reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                return new DailyNutritionSummary
                {
                    TotalCalories = reader.GetDouble(0),
                    TotalCarbs = reader.GetDouble(1),
                    TotalProtein = reader.GetDouble(2),
                    TotalFat = reader.GetDouble(3),
                    MealCount = reader.GetInt32(4)
                };
            }

            return new DailyNutritionSummary();
        }
    }
}
