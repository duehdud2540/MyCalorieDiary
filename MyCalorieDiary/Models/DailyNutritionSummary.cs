namespace MyCalorieDiary.Models
{
    public class DailyNutritionSummary
    {
        public double TotalCalories { get; set; }
        public double TotalCarbs { get; set; }
        public double TotalProtein { get; set; }
        public double TotalFat { get; set; }
        public int MealCount { get; set; }

        public double CarbsCalories => TotalCarbs * 4.0;
        public double ProteinCalories => TotalProtein * 4.0;
        public double FatCalories => TotalFat * 9.0;

        public double TotalMacroCalories => CarbsCalories + ProteinCalories + FatCalories;

        public double CarbsPercentage => TotalMacroCalories > 0 ? (CarbsCalories / TotalMacroCalories) * 100.0 : 0;
        public double ProteinPercentage => TotalMacroCalories > 0 ? (ProteinCalories / TotalMacroCalories) * 100.0 : 0;
        public double FatPercentage => TotalMacroCalories > 0 ? (FatCalories / TotalMacroCalories) * 100.0 : 0;
    }
}
