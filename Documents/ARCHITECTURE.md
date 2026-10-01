### 1. Архітектурний підхід

TripFlow — настільний застосунок WPF (C#, .NET) з локальною базою MS SQL Server LocalDB. Обрано **шарову архітектуру з шаблоном MVVM**:

```
View (XAML) → ViewModel → BLL (сервіси) → DAL (репозиторії, ADO.NET) → LocalDB
```

Головні правила:

* кожен шар звертається лише до наступного шару вниз;
* уся бізнес-логіка та валідація знаходиться в BLL, а не у вікнах;
* BLL оголошує інтерфейси репозиторіїв, а DAL їх реалізує, тому сервіси можна тестувати без бази даних;
* об'єкти створюються в одному місці через Dependency Injection (`App.xaml.cs`).



### 2. Структура рішення

```
TripFlow.sln
├── src/
│   ├── TripFlow.Models/   сутності та enum-и
│   ├── TripFlow.BLL/      сервіси, інтерфейси репозиторіїв, OperationResult
│   ├── TripFlow.DAL/      репозиторії, SqlConnectionFactory, DatabaseInitializer
│   └── TripFlow.UI/       Views, ViewModels, діалоги, appsettings.json
├── tests/
│   └── TripFlow.Tests/    модульні та інтеграційні тести
├── Documents/
└── UI/
```

Залежності: `UI → BLL`, `DAL → BLL`, `BLL → Models`, `DAL → Models`, `Tests → BLL, DAL`. UI посилається на DAL лише для підключення реалізацій у DI.

Основні пакети: `Microsoft.Data.SqlClient` (БД), `CommunityToolkit.Mvvm` (MVVM), `Microsoft.Extensions.DependencyInjection` (DI), `Serilog` (логи), `xUnit` + `Moq` (тести), PDF-бібліотека (QuestPDF або PDFsharp).
