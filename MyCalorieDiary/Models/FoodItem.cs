using System;

namespace MyCalorieDiary.Models
{
    public class FoodItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public double ServingSize { get; set; } = 1;
        public string ServingUnit { get; set; } = "인분";
        public double Calories { get; set; }
        public double Carbohydrates { get; set; }
        public double Protein { get; set; }
        public double Fat { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string DisplayText => $"{Name} ({Calories:F0} kcal | 탄: {Carbohydrates:F1}g, 단: {Protein:F1}g, 지: {Fat:F1}g / {PortionHelper.FormatServingSizeWithUnit(ServingUnit)})";
        
        public string NutrientsSummary => $"탄수화물 {Carbohydrates:F1}g · 단백질 {Protein:F1}g · 지방 {Fat:F1}g";
    }
}
