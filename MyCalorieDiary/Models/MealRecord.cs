using System;

namespace MyCalorieDiary.Models
{
    public class MealRecord
    {
        public int Id { get; set; }
        public int? FoodId { get; set; }
        public string FoodName { get; set; } = string.Empty;
        public DateTime MealDate { get; set; } = DateTime.Now;
        public string MealType { get; set; } = "점심"; // 아침, 점심, 저녁, 간식
        public double Amount { get; set; } = 1.0;
        public string ServingUnit { get; set; } = "인분";
        public double TotalCalories { get; set; }
        public double TotalCarbs { get; set; }
        public double TotalProtein { get; set; }
        public double TotalFat { get; set; }

        public string FormattedDate => MealDate.ToString("yyyy-MM-dd HH:mm");
        public string FormattedDateOnly => MealDate.ToString("yyyy-MM-dd");
        public string FormattedTimeOnly => MealDate.ToString("HH:mm");
        public string PortionDisplay => PortionHelper.FormatPortion(Amount, ServingUnit);
        public string SummaryText => $"{FoodName} ({PortionDisplay}) - {TotalCalories:F0} kcal";
    }
}
