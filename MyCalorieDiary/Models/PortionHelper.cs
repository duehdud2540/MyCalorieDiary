using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace MyCalorieDiary.Models
{
    /// <summary>
    /// 음식 섭취량 및 단위 포맷팅을 지원하는 헬퍼 클래스
    /// </summary>
    public static class PortionHelper
    {
        private static readonly Regex UnitRegex = new(@"^(\d+(?:\.\d+)?)\s*(.*)$", RegexOptions.Compiled);

        /// <summary>
        /// 배율(amount)과 단위(servingUnit)를 곱해 깔끔한 섭취량 문자열을 반환합니다.
        /// 예:
        /// - amount = 2, unit = "1스쿱" => "2스쿱"
        /// - amount = 2, unit = "100g" => "200g"
        /// - amount = 1.5, unit = "100g" => "150g"
        /// - amount = 0.5, unit = "1스쿱" => "0.5스쿱"
        /// - amount = 2, unit = "스쿱" => "2스쿱"
        /// - amount = 2, unit = "1인분" => "2인분"
        /// - amount = 2, unit = "인분" => "2인분"
        /// - amount = 2, unit = "200ml" => "400ml"
        /// </summary>
        public static string FormatPortion(double amount, string? servingUnit)
        {
            if (string.IsNullOrWhiteSpace(servingUnit))
            {
                return $"{amount:0.##}";
            }

            string trimmed = servingUnit.Trim();
            var match = UnitRegex.Match(trimmed);

            if (match.Success)
            {
                string numberPart = match.Groups[1].Value;
                string textPart = match.Groups[2].Value.Trim();

                if (double.TryParse(numberPart, NumberStyles.Float, CultureInfo.InvariantCulture, out double baseNumber))
                {
                    double total = amount * baseNumber;
                    return $"{total:0.##}{textPart}";
                }
            }

            // 앞부분에 숫자가 없는 경우 (예: "스쿱", "g", "인분", "개")
            return $"{amount:0.##}{trimmed}";
        }

        /// <summary>
        /// 1회 기준 단위 텍스트를 포맷팅합니다.
        /// 단위에 이미 숫자가 포함되어 있으면(예: "100g", "1스쿱") 그대로 반환하고,
        /// 숫자가 없으면(예: "인분", "개") 앞에 '1'을 붙여 반환합니다.
        /// </summary>
        public static string FormatServingSizeWithUnit(string? servingUnit)
        {
            if (string.IsNullOrWhiteSpace(servingUnit))
            {
                return "1인분";
            }

            string trimmed = servingUnit.Trim();
            if (UnitRegex.IsMatch(trimmed))
            {
                return trimmed;
            }

            return $"1{trimmed}";
        }
    }
}
