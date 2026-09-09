using System;
using System.IO;
using System.Linq;
using MyCalorieDiary.Models;
using MyCalorieDiary.Services;
using MyCalorieDiary.ViewModels;
using Xunit;

namespace MyCalorieDiary.Tests
{
    public class CalorieDiaryTests
    {
        [Fact]
        public void Database_ShouldInitializeAndSeedFoods()
        {
            var db = new DatabaseService();
            Assert.True(File.Exists(db.DbPath), "SQLite db file should exist");

            var foods = db.GetAllFoods();
            Assert.NotEmpty(foods);
            Assert.Contains(foods, f => f.Name.Contains("닭가슴살"));
            Assert.Contains(foods, f => f.Name.Contains("쌀밥"));
        }

        [Fact]
        public void Database_ShouldAddAndQueryNewFood()
        {
            var db = new DatabaseService();
            string testName = "테스트 음식 " + Guid.NewGuid().ToString("N")[..6];

            var food = new FoodItem
            {
                Name = testName,
                ServingSize = 1.0,
                ServingUnit = "그릇",
                Calories = 350,
                Carbohydrates = 45,
                Protein = 20,
                Fat = 10
            };

            int id = db.AddFood(food);
            Assert.True(id > 0);

            var foods = db.GetAllFoods();
            var added = foods.FirstOrDefault(f => f.Id == id);
            Assert.NotNull(added);
            Assert.Equal(testName, added.Name);
            Assert.Equal(350, added.Calories);
            Assert.Equal(45, added.Carbohydrates);
            Assert.Equal(20, added.Protein);
            Assert.Equal(10, added.Fat);

            // Cleanup
            db.DeleteFood(id);
            var afterDelete = db.GetAllFoods().FirstOrDefault(f => f.Id == id);
            Assert.Null(afterDelete);
        }

        [Fact]
        public void Database_ShouldAddMealRecordAndCalculateDailySummary()
        {
            var db = new DatabaseService();
            var today = DateTime.Today;

            var record1 = new MealRecord
            {
                FoodName = "아침 테스트",
                MealDate = today.AddHours(8),
                MealType = "아침",
                Amount = 1.0,
                ServingUnit = "인분",
                TotalCalories = 300,
                TotalCarbs = 50,
                TotalProtein = 15,
                TotalFat = 5
            };

            var record2 = new MealRecord
            {
                FoodName = "점심 테스트",
                MealDate = today.AddHours(12),
                MealType = "점심",
                Amount = 2.0,
                ServingUnit = "인분",
                TotalCalories = 500,
                TotalCarbs = 60,
                TotalProtein = 40,
                TotalFat = 10
            };

            int id1 = db.AddMealRecord(record1);
            int id2 = db.AddMealRecord(record2);

            var logs = db.GetMealRecords(today);
            Assert.True(logs.Count >= 2);

            var summary = db.GetDailySummary(today);
            Assert.True(summary.TotalCalories >= 800);
            Assert.True(summary.TotalCarbs >= 110);
            Assert.True(summary.TotalProtein >= 55);
            Assert.True(summary.TotalFat >= 15);

            // Verify macro ratio calculations
            Assert.True(summary.CarbsPercentage > 0);
            Assert.True(summary.ProteinPercentage > 0);
            Assert.True(summary.FatPercentage > 0);

            // Cleanup
            db.DeleteMealRecord(id1);
            db.DeleteMealRecord(id2);
        }

        [Fact]
        public void ViewModel_PortionChange_ShouldUpdatePreviewCalculations()
        {
            var vm = new MainViewModel();

            var sampleFood = new FoodItem
            {
                Name = "단백질 쉐이크",
                Calories = 200,
                Carbohydrates = 10,
                Protein = 30,
                Fat = 4
            };

            vm.SelectedFood = sampleFood;
            vm.IntakeAmount = 1.5;

            Assert.Equal(300, vm.PreviewCalories);
            Assert.Equal(15, vm.PreviewCarbs);
            Assert.Equal(45, vm.PreviewProtein);
            Assert.Equal(6, vm.PreviewFat);
        }

        [Fact]
        public void ViewModel_AutoCalculateNewCalories_ShouldWorkCorrectly()
        {
            var vm = new MainViewModel();
            vm.NewFoodCarbsText = "50";   // 50 * 4 = 200
            vm.NewFoodProteinText = "25"; // 25 * 4 = 100
            vm.NewFoodFatText = "10";     // 10 * 9 = 90
            // Total should be 390

            vm.AutoCalculateNewCaloriesCommand.Execute(null);

            Assert.Equal("390", vm.NewFoodCaloriesText);
        }

        [Theory]
        [InlineData(2.0, "1스쿱", "2스쿱")]
        [InlineData(2.0, "100g", "200g")]
        [InlineData(1.5, "100g", "150g")]
        [InlineData(0.5, "1스쿱", "0.5스쿱")]
        [InlineData(2.0, "200ml", "400ml")]
        [InlineData(2.0, "1공기", "2공기")]
        [InlineData(2.0, "1개", "2개")]
        [InlineData(2.0, "1인분", "2인분")]
        [InlineData(2.0, "스쿱", "2스쿱")]
        [InlineData(2.0, "인분", "2인분")]
        [InlineData(2.0, "개", "2개")]
        [InlineData(1.0, "100g", "100g")]
        [InlineData(2.0, null, "2")]
        [InlineData(2.0, "", "2")]
        public void PortionHelper_FormatPortion_ShouldFormatCorrectly(double amount, string? unit, string expected)
        {
            string actual = PortionHelper.FormatPortion(amount, unit);
            Assert.Equal(expected, actual);
        }

        [Fact]
        public void MealRecord_PortionDisplay_ShouldProduceExpectedResult()
        {
            var record1 = new MealRecord { Amount = 2.0, ServingUnit = "1스쿱" };
            Assert.Equal("2스쿱", record1.PortionDisplay);

            var record2 = new MealRecord { Amount = 2.0, ServingUnit = "100g" };
            Assert.Equal("200g", record2.PortionDisplay);

            var record3 = new MealRecord { Amount = 1.5, ServingUnit = "100g" };
            Assert.Equal("150g", record3.PortionDisplay);
        }

        [Fact]
        public void ViewModel_PreviewPortionDisplay_ShouldUpdateWhenFoodOrAmountChanges()
        {
            var vm = new MainViewModel();
            var food = new FoodItem
            {
                Name = "프로틴 쉐이크",
                ServingUnit = "1스쿱",
                Calories = 120,
                Carbohydrates = 3,
                Protein = 24,
                Fat = 1.5
            };

            vm.SelectedFood = food;
            vm.IntakeAmount = 2.0;

            Assert.Equal("2스쿱", vm.PreviewPortionDisplay);

            vm.IntakeAmount = 3.0;
            Assert.Equal("3스쿱", vm.PreviewPortionDisplay);
        }
    }
}
