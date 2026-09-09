# 🥗 My Calorie Diary (식단 & 칼로리 다이어리)

WPF(.NET 10)와 SQLite를 기반으로 개발된 일일 칼로리 및 영양성분(탄수화물, 단백질, 지방) 계산 및 기록 프로그램입니다.

---

## 📌 주요 기능

### 1. 🍽️ 먹은 음식 입력 (식단 기록)
- **드롭다운(ComboBox) 형식**으로 데이터베이스에 등록된 음식을 간편하게 선택
- **섭취량(인분/배수)** 입력 및 빠른 선택 버튼(0.5, 1.0, 1.5, 2.0인분)
- **실시간 영양성분 자동 계산**: 음식을 고르고 양을 입력하면 섭취할 칼로리와 탄·단·지가 즉시 미리보기로 계산
- 선택한 일자의 식사 기록 목록 및 삭제 기능 지원

### 2. ➕ 음식 데이터 등록 (SQLite 로컬 DB 저장)
- **음식명, 1회 기준 단위, 탄수화물(g), 단백질(g), 지방(g), 칼로리(kcal)** 입력
- **[⚡ 탄·단·지 자동 계산]** 버튼을 통해 `(탄수화물×4) + (단백질×4) + (지방×9)` 공식을 사용한 칼로리 자동 환산 기능 제공
- 등록 시 SQLite 로컬 `.db` (`calorie_diary.db`) 파일에 영구 저장되며, [먹은 음식 입력] 드롭다운에 즉시 반영
- 데이터베이스에 등록된 전체 음식 목록 조회, 검색(필터링), 개별 삭제 기능

### 3. 📅 먹었던 기록 확인 (통계 & 히스토리)
- SQLite에 저장된 모든 과거 식사 기록을 일자별/기간별로 조회
- **기간 프리셋 필터**: 오늘 / 최근 7일 / 최근 30일 / 전체 / 사용자 지정 기간(DatePicker)
- **식사 구분 필터**: 아침 / 점심 / 저녁 / 간식
- 해당 기간 동안의 **총 섭취 칼로리 및 탄·단·지 총합 통계** 자동 집계

### 4. 📊 대시보드 (오늘 섭취량 요약 카드)
- 당일 총 섭취 칼로리 및 목표 칼로리 대비 프로그레스 바 표시
- 탄수화물, 단백질, 지방 섭취량(g) 및 칼로리 기여도 표시
- 탄·단·지 에너지 비율(%) 실시간 계산 및 표시

---

## 🗄️ SQLite 데이터베이스 구조 (`calorie_diary.db`)

프로그램 실행 시 실행 폴더에 `calorie_diary.db` 파일이 자동 생성되며 다음 테이블로 구성됩니다:

1. **`Foods` (음식 마스터 테이블)**
   - `Id` (INTEGER, PK)
   - `Name` (TEXT): 음식 이름
   - `ServingSize` (REAL): 1회 제공량
   - `ServingUnit` (TEXT): 단위 (예: 인분, 100g, 개)
   - `Calories` (REAL): 칼로리 (kcal)
   - `Carbohydrates` (REAL): 탄수화물 (g)
   - `Protein` (REAL): 단백질 (g)
   - `Fat` (REAL): 지방 (g)
   - `CreatedAt` (TEXT): 등록 일시

2. **`MealLogs` (식사 기록 테이블)**
   - `Id` (INTEGER, PK)
   - `FoodId` (INTEGER, FK)
   - `FoodName` (TEXT): 섭취한 음식명
   - `MealDate` (TEXT): 섭취 날짜 및 시간 (`yyyy-MM-dd HH:mm:ss`)
   - `MealType` (TEXT): 식사 구분 (아침/점심/저녁/간식)
   - `Amount` (REAL): 섭취량 (배수/인분)
   - `ServingUnit` (TEXT): 단위
   - `TotalCalories` (REAL): 계산된 총 칼로리
   - `TotalCarbs` (REAL): 계산된 탄수화물
   - `TotalProtein` (REAL): 계산된 단백질
   - `TotalFat` (REAL): 계산된 지방

*※ 최초 실행 시 밥, 닭가슴살, 달걀, 바나나, 고구마 등 대표적인 일상 식품 14종이 기본 시드 데이터로 자동 등록되어 바로 사용할 수 있습니다.*

---

## 🚀 실행 및 빌드 방법

### 요구 사항
- .NET 10 SDK (Windows 환경)

### 빌드
```bash
dotnet build
```

### 테스트 실행
```bash
dotnet test
```

### 프로그램 실행
Visual Studio에서 실행하거나 터미널에서 다음 명령어로 실행합니다:
```bash
dotnet run --project MyCalorieDiary\MyCalorieDiary.csproj
```
또는 빌드된 실행 파일 직접 실행:
```
MyCalorieDiary\bin\Debug\net10.0-windows\MyCalorieDiary.exe
```
