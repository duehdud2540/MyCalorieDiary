using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using MyCalorieDiary.Models;
using MyCalorieDiary.Services;

namespace MyCalorieDiary.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly DatabaseService _dbService;

        // Current Selected Date for Daily View
        private DateTime _selectedDate = DateTime.Today;
        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetField(ref _selectedDate, value))
                {
                    OnPropertyChanged(nameof(FormattedSelectedDate));
                    OnPropertyChanged(nameof(IsToday));
                    LoadDailyData();
                }
            }
        }

        public string FormattedSelectedDate => _selectedDate.ToString("yyyy년 MM월 dd일 (ddd)");
        public bool IsToday => _selectedDate.Date == DateTime.Today;

        // Daily Nutrition Summary
        private DailyNutritionSummary _dailySummary = new();
        public DailyNutritionSummary DailySummary
        {
            get => _dailySummary;
            set => SetField(ref _dailySummary, value);
        }

        private double _dailyCalorieGoal = 2000;
        public double DailyCalorieGoal
        {
            get => _dailyCalorieGoal;
            set
            {
                if (SetField(ref _dailyCalorieGoal, value))
                {
                    OnPropertyChanged(nameof(RemainingCalories));
                    OnPropertyChanged(nameof(CalorieProgressPercentage));
                }
            }
        }

        public double RemainingCalories => Math.Max(0, DailyCalorieGoal - DailySummary.TotalCalories);
        public double CalorieProgressPercentage => DailyCalorieGoal > 0 ? Math.Min(100.0, (DailySummary.TotalCalories / DailyCalorieGoal) * 100.0) : 0;

        // Food Database List & Selected Food for Logging
        public ObservableCollection<FoodItem> Foods { get; } = new();

        private ObservableCollection<FoodItem> _filteredFoods = new();
        public ObservableCollection<FoodItem> FilteredFoods
        {
            get => _filteredFoods;
            set => SetField(ref _filteredFoods, value);
        }

        private string _foodSearchText = string.Empty;
        public string FoodSearchText
        {
            get => _foodSearchText;
            set
            {
                if (SetField(ref _foodSearchText, value))
                {
                    FilterFoodsList();
                }
            }
        }

        // =====================
        // Tab 1: Log Consumed Meal
        // =====================
        private FoodItem? _selectedFood;
        public FoodItem? SelectedFood
        {
            get => _selectedFood;
            set
            {
                if (SetField(ref _selectedFood, value))
                {
                    UpdatePortionCalculations();
                }
            }
        }

        private string _mealType = "점심";
        public string MealType
        {
            get => _mealType;
            set => SetField(ref _mealType, value);
        }

        public ObservableCollection<string> MealTypes { get; } = new() { "아침", "점심", "저녁", "간식" };

        private double _intakeAmount = 1.0;
        public double IntakeAmount
        {
            get => _intakeAmount;
            set
            {
                if (SetField(ref _intakeAmount, Math.Round(Math.Max(0.01, value), 2)))
                {
                    UpdatePortionCalculations();
                }
            }
        }

        private DateTime _intakeDateTime = DateTime.Now;
        public DateTime IntakeDateTime
        {
            get => _intakeDateTime;
            set => SetField(ref _intakeDateTime, value);
        }

        // Realtime Preview Properties
        private double _previewCalories;
        public double PreviewCalories
        {
            get => _previewCalories;
            set => SetField(ref _previewCalories, value);
        }

        private double _previewCarbs;
        public double PreviewCarbs
        {
            get => _previewCarbs;
            set => SetField(ref _previewCarbs, value);
        }

        private double _previewProtein;
        public double PreviewProtein
        {
            get => _previewProtein;
            set => SetField(ref _previewProtein, value);
        }

        private double _previewFat;
        public double PreviewFat
        {
            get => _previewFat;
            set => SetField(ref _previewFat, value);
        }

        public string PreviewPortionDisplay => PortionHelper.FormatPortion(IntakeAmount, SelectedFood?.ServingUnit);

        public ObservableCollection<MealRecord> TodayMealRecords { get; } = new();

        // =====================
        // Tab 2: Add Food to SQLite DB
        // =====================
        private string _newFoodName = string.Empty;
        public string NewFoodName
        {
            get => _newFoodName;
            set => SetField(ref _newFoodName, value);
        }

        private string _newFoodUnit = "인분";
        public string NewFoodUnit
        {
            get => _newFoodUnit;
            set => SetField(ref _newFoodUnit, value);
        }

        private string _newFoodCarbsText = string.Empty;
        public string NewFoodCarbsText
        {
            get => _newFoodCarbsText;
            set => SetField(ref _newFoodCarbsText, value);
        }

        private string _newFoodProteinText = string.Empty;
        public string NewFoodProteinText
        {
            get => _newFoodProteinText;
            set => SetField(ref _newFoodProteinText, value);
        }

        private string _newFoodFatText = string.Empty;
        public string NewFoodFatText
        {
            get => _newFoodFatText;
            set => SetField(ref _newFoodFatText, value);
        }

        private string _newFoodCaloriesText = string.Empty;
        public string NewFoodCaloriesText
        {
            get => _newFoodCaloriesText;
            set => SetField(ref _newFoodCaloriesText, value);
        }

        private string _foodStatusMessage = string.Empty;
        public string FoodStatusMessage
        {
            get => _foodStatusMessage;
            set => SetField(ref _foodStatusMessage, value);
        }

        // =====================
        // Tab 3: History Records & Analysis
        // =====================
        public ObservableCollection<MealRecord> HistoryRecords { get; } = new();

        private DateTime _historyStartDate = DateTime.Today.AddDays(-7);
        public DateTime HistoryStartDate
        {
            get => _historyStartDate;
            set
            {
                if (SetField(ref _historyStartDate, value))
                {
                    LoadHistoryRecords();
                }
            }
        }

        private DateTime _historyEndDate = DateTime.Today;
        public DateTime HistoryEndDate
        {
            get => _historyEndDate;
            set
            {
                if (SetField(ref _historyEndDate, value))
                {
                    LoadHistoryRecords();
                }
            }
        }

        private string _historyMealFilter = "전체";
        public string HistoryMealFilter
        {
            get => _historyMealFilter;
            set
            {
                if (SetField(ref _historyMealFilter, value))
                {
                    LoadHistoryRecords();
                }
            }
        }

        public ObservableCollection<string> HistoryMealFilters { get; } = new() { "전체", "아침", "점심", "저녁", "간식" };

        private double _historyTotalCalories;
        public double HistoryTotalCalories
        {
            get => _historyTotalCalories;
            set => SetField(ref _historyTotalCalories, value);
        }

        private double _historyTotalCarbs;
        public double HistoryTotalCarbs
        {
            get => _historyTotalCarbs;
            set => SetField(ref _historyTotalCarbs, value);
        }

        private double _historyTotalProtein;
        public double HistoryTotalProtein
        {
            get => _historyTotalProtein;
            set => SetField(ref _historyTotalProtein, value);
        }

        private double _historyTotalFat;
        public double HistoryTotalFat
        {
            get => _historyTotalFat;
            set => SetField(ref _historyTotalFat, value);
        }

        public string DbFilePath => _dbService.DbPath;

        // Commands
        public ICommand PreviousDayCommand { get; }
        public ICommand NextDayCommand { get; }
        public ICommand GoToTodayCommand { get; }
        public ICommand SetPortionCommand { get; }
        public ICommand AddMealLogCommand { get; }
        public ICommand DeleteMealLogCommand { get; }
        public ICommand AutoCalculateNewCaloriesCommand { get; }
        public ICommand AddNewFoodCommand { get; }
        public ICommand DeleteFoodCommand { get; }
        public ICommand RefreshAllCommand { get; }
        public ICommand SetHistoryRangePresetCommand { get; }
        public ICommand OpenDbFolderCommand { get; }

        public MainViewModel()
        {
            _dbService = new DatabaseService();

            // Date navigation
            PreviousDayCommand = new RelayCommand(() => SelectedDate = SelectedDate.AddDays(-1));
            NextDayCommand = new RelayCommand(() => SelectedDate = SelectedDate.AddDays(1));
            GoToTodayCommand = new RelayCommand(() => SelectedDate = DateTime.Today);

            // Portion presets (e.g. 0.5, 1.0, 1.5, 2.0)
            SetPortionCommand = new RelayCommand(param =>
            {
                if (param != null && double.TryParse(param.ToString(), out double p))
                {
                    IntakeAmount = p;
                }
            });

            // Tab 1 Commands
            AddMealLogCommand = new RelayCommand(ExecuteAddMealLog, CanAddMealLog);
            DeleteMealLogCommand = new RelayCommand(ExecuteDeleteMealLog);

            // Tab 2 Commands
            AutoCalculateNewCaloriesCommand = new RelayCommand(ExecuteAutoCalculateCalories);
            AddNewFoodCommand = new RelayCommand(ExecuteAddNewFood);
            DeleteFoodCommand = new RelayCommand(ExecuteDeleteFood);

            // Tab 3 Commands
            RefreshAllCommand = new RelayCommand(LoadAllData);
            SetHistoryRangePresetCommand = new RelayCommand(ExecuteSetHistoryPreset);
            OpenDbFolderCommand = new RelayCommand(() =>
            {
                try
                {
                    string dir = System.IO.Path.GetDirectoryName(DbFilePath) ?? AppDomain.CurrentDomain.BaseDirectory;
                    if (System.IO.File.Exists(DbFilePath))
                    {
                        System.Diagnostics.Process.Start("explorer.exe", $"/select,\"{DbFilePath}\"");
                    }
                    else
                    {
                        System.Diagnostics.Process.Start("explorer.exe", dir);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"폴더 열기 실패: {ex.Message}", "오류", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            });

            // Initial load
            LoadAllData();
        }

        public void LoadAllData()
        {
            LoadFoods();
            LoadDailyData();
            LoadHistoryRecords();
        }

        private void LoadFoods()
        {
            var list = _dbService.GetAllFoods();
            Foods.Clear();
            foreach (var item in list)
            {
                Foods.Add(item);
            }
            FilterFoodsList();

            if (SelectedFood == null && Foods.Count > 0)
            {
                SelectedFood = Foods.FirstOrDefault();
            }
        }

        private void FilterFoodsList()
        {
            FilteredFoods.Clear();
            var query = Foods.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(FoodSearchText))
            {
                query = query.Where(f => f.Name.Contains(FoodSearchText, StringComparison.OrdinalIgnoreCase));
            }

            foreach (var food in query)
            {
                FilteredFoods.Add(food);
            }
        }

        public void LoadDailyData()
        {
            // Daily meal records
            TodayMealRecords.Clear();
            var logs = _dbService.GetMealRecords(SelectedDate);
            foreach (var log in logs)
            {
                TodayMealRecords.Add(log);
            }

            // Daily summary
            DailySummary = _dbService.GetDailySummary(SelectedDate);
            OnPropertyChanged(nameof(RemainingCalories));
            OnPropertyChanged(nameof(CalorieProgressPercentage));
        }

        private void UpdatePortionCalculations()
        {
            OnPropertyChanged(nameof(PreviewPortionDisplay));

            if (SelectedFood == null)
            {
                PreviewCalories = 0;
                PreviewCarbs = 0;
                PreviewProtein = 0;
                PreviewFat = 0;
                return;
            }

            PreviewCalories = Math.Round(SelectedFood.Calories * IntakeAmount, 1);
            PreviewCarbs = Math.Round(SelectedFood.Carbohydrates * IntakeAmount, 1);
            PreviewProtein = Math.Round(SelectedFood.Protein * IntakeAmount, 1);
            PreviewFat = Math.Round(SelectedFood.Fat * IntakeAmount, 1);
        }

        private bool CanAddMealLog()
        {
            return SelectedFood != null && IntakeAmount > 0;
        }

        private void ExecuteAddMealLog()
        {
            if (SelectedFood == null)
            {
                MessageBox.Show("음식을 드롭다운에서 선택해 주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (IntakeAmount <= 0)
            {
                MessageBox.Show("섭취량을 0보다 크게 입력해 주세요.", "알림", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Record date uses SelectedDate's date part combined with current time or IntakeDateTime
            var recordDate = new DateTime(
                SelectedDate.Year,
                SelectedDate.Month,
                SelectedDate.Day,
                IntakeDateTime.Hour,
                IntakeDateTime.Minute,
                IntakeDateTime.Second
            );

            var newRecord = new MealRecord
            {
                FoodId = SelectedFood.Id,
                FoodName = SelectedFood.Name,
                MealDate = recordDate,
                MealType = MealType,
                Amount = IntakeAmount,
                ServingUnit = SelectedFood.ServingUnit,
                TotalCalories = PreviewCalories,
                TotalCarbs = PreviewCarbs,
                TotalProtein = PreviewProtein,
                TotalFat = PreviewFat
            };

            _dbService.AddMealRecord(newRecord);

            // Refresh views
            LoadDailyData();
            LoadHistoryRecords();

            string formattedPortion = PortionHelper.FormatPortion(IntakeAmount, SelectedFood.ServingUnit);
            FoodStatusMessage = $"'{SelectedFood.Name}' {formattedPortion} 등록 완료 ({PreviewCalories:F0} kcal)";
        }

        private void ExecuteDeleteMealLog(object? param)
        {
            if (param is MealRecord record)
            {
                var result = MessageBox.Show(
                    $"'{record.FoodName}' ({record.FormattedDate}) 기록을 삭제하시겠습니까?",
                    "기록 삭제 확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _dbService.DeleteMealRecord(record.Id);
                    LoadDailyData();
                    LoadHistoryRecords();
                }
            }
        }

        private void ExecuteAutoCalculateCalories()
        {
            double carbs = double.TryParse(NewFoodCarbsText, out double c) ? c : 0;
            double protein = double.TryParse(NewFoodProteinText, out double p) ? p : 0;
            double fat = double.TryParse(NewFoodFatText, out double f) ? f : 0;

            double calc = (carbs * 4.0) + (protein * 4.0) + (fat * 9.0);
            NewFoodCaloriesText = Math.Round(calc, 1).ToString("0.#");
        }

        private void ExecuteAddNewFood()
        {
            if (string.IsNullOrWhiteSpace(NewFoodName))
            {
                MessageBox.Show("음식 이름을 입력해 주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(NewFoodCarbsText, out double carbs) || carbs < 0)
            {
                MessageBox.Show("탄수화물(g)을 올바른 숫자로 입력해 주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(NewFoodProteinText, out double protein) || protein < 0)
            {
                MessageBox.Show("단백질(g)을 올바른 숫자로 입력해 주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(NewFoodFatText, out double fat) || fat < 0)
            {
                MessageBox.Show("지방(g)을 올바른 숫자로 입력해 주세요.", "입력 오류", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!double.TryParse(NewFoodCaloriesText, out double calories) || calories < 0)
            {
                // Auto calculate if empty or 0
                calories = (carbs * 4.0) + (protein * 4.0) + (fat * 9.0);
            }

            var food = new FoodItem
            {
                Name = NewFoodName.Trim(),
                ServingUnit = string.IsNullOrWhiteSpace(NewFoodUnit) ? "인분" : NewFoodUnit.Trim(),
                ServingSize = 1.0,
                Carbohydrates = Math.Round(carbs, 1),
                Protein = Math.Round(protein, 1),
                Fat = Math.Round(fat, 1),
                Calories = Math.Round(calories, 1)
            };

            int newId = _dbService.AddFood(food);
            food.Id = newId;

            // Clear inputs
            NewFoodName = string.Empty;
            NewFoodCarbsText = string.Empty;
            NewFoodProteinText = string.Empty;
            NewFoodFatText = string.Empty;
            NewFoodCaloriesText = string.Empty;

            LoadFoods();
            SelectedFood = Foods.FirstOrDefault(f => f.Id == newId);

            MessageBox.Show($"'{food.Name}'(이)가 데이터베이스에 성공적으로 추가되었습니다!", "등록 완료", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ExecuteDeleteFood(object? param)
        {
            if (param is FoodItem food)
            {
                var result = MessageBox.Show(
                    $"'{food.Name}' 음식을 목록에서 삭제하시겠습니까?",
                    "음식 삭제 확인",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    _dbService.DeleteFood(food.Id);
                    LoadFoods();
                }
            }
        }

        private void ExecuteSetHistoryPreset(object? param)
        {
            if (param is string preset)
            {
                switch (preset)
                {
                    case "Today":
                        HistoryStartDate = DateTime.Today;
                        HistoryEndDate = DateTime.Today;
                        break;
                    case "7Days":
                        HistoryStartDate = DateTime.Today.AddDays(-6);
                        HistoryEndDate = DateTime.Today;
                        break;
                    case "30Days":
                        HistoryStartDate = DateTime.Today.AddDays(-29);
                        HistoryEndDate = DateTime.Today;
                        break;
                    case "All":
                        HistoryStartDate = DateTime.Today.AddYears(-1);
                        HistoryEndDate = DateTime.Today;
                        break;
                }
            }
        }

        public void LoadHistoryRecords()
        {
            HistoryRecords.Clear();
            var records = _dbService.GetMealRecordsByRange(HistoryStartDate, HistoryEndDate);

            if (HistoryMealFilter != "전체")
            {
                records = records.Where(r => r.MealType == HistoryMealFilter).ToList();
            }

            double totalCal = 0;
            double totalC = 0;
            double totalP = 0;
            double totalF = 0;

            foreach (var r in records)
            {
                HistoryRecords.Add(r);
                totalCal += r.TotalCalories;
                totalC += r.TotalCarbs;
                totalP += r.TotalProtein;
                totalF += r.TotalFat;
            }

            HistoryTotalCalories = Math.Round(totalCal, 1);
            HistoryTotalCarbs = Math.Round(totalC, 1);
            HistoryTotalProtein = Math.Round(totalP, 1);
            HistoryTotalFat = Math.Round(totalF, 1);
        }
    }
}
