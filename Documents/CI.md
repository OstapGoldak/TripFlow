# Неперервна інтеграція (CI)

TripFlow автоматично збирається й тестується в **GitHub Actions** на кожен push у `main` і на кожен pull request. Pull request можна зливати лише тоді, коли всі перевірки зелені.

## Workflow

| Файл | Назва в Actions | Що перевіряє |
|---|---|---|
| `.github/workflows/ci.yml` | **CI** | Збирання рішення, модульні тести, інтеграційні тести на LocalDB |
| `.github/workflows/quality.yml` | **Quality** | Форматування коду, покриття тестами, вразливі NuGet-пакети |

Обидва запускаються:
- на push у `main`;
- на кожен pull request у `main`;
- вручну: вкладка **Actions** → обрати workflow → **Run workflow**.

Новий push у ту саму гілку скасовує попередній незавершений запуск.

## CI (`ci.yml`)

Усі кроки виконуються на `windows-latest`: проєкт інтерфейсу — WPF (`net10.0-windows`), його можна зібрати лише на Windows.

### Job 1. Build and unit tests

1. Отримання коду (`actions/checkout`).
2. Встановлення .NET 10 (`actions/setup-dotnet`).
3. `dotnet restore` — завантаження NuGet-пакетів.
4. `dotnet build --configuration Release` — збирання всіх проєктів рішення.
5. `dotnet test --filter "Category!=Integration"` — модульні тести без бази даних.
6. Результати тестів (`.trx`) зберігаються як артефакт **unit-test-results**.

### Job 2. Integration tests (LocalDB)

Запускається лише після успішного першого job (`needs: build-and-test`).

1. Отримання коду, встановлення .NET 10.
2. Запуск SQL Server LocalDB (якщо його немає на машині, встановлюється через Chocolatey).
3. `dotnet test --filter "Category=Integration"` — тести репозиторіїв на тимчасовій базі (див. `LocalDbFixture`).
4. Результати зберігаються як артефакт **integration-test-results**.

## Quality (`quality.yml`)

| Перевірка | Команда | Що буде при порушенні |
|---|---|---|
| Форматування коду | `dotnet format whitespace --verify-no-changes` | Запуск червоний. Виправлення: `dotnet format whitespace TripFlow.sln` |
| Покриття коду | `dotnet test --collect:"XPlat Code Coverage"` + ReportGenerator | Підсумок у **Summary** запуску, повний HTML-звіт в артефакті **coverage-report** |
| Вразливі пакети | `dotnet list package --vulnerable --include-transitive` | Попередження в запуску. Виправлення вразливостей заплановано на етап 9 |

## Як читати результат

- **У pull request:** внизу сторінки блок перевірок. Зелена галочка — усе пройшло, червоний хрестик — ні. **Details** відкриває лог.
- **У вкладці Actions:** список усіх запусків. Відкрий запуск → обери job → розгорни червоний крок, щоб побачити помилку.
- **Summary запуску:** покриття коду й перелік вразливих пакетів.
- **Artifacts** (унизу сторінки запуску): результати тестів і HTML-звіт покриття для завантаження.

## Що робити, якщо запуск червоний

| Червоний крок | Що робити |
|---|---|
| Build | Зібрати локально `dotnet build`, виправити помилки компіляції |
| Unit tests / Integration tests | Запустити `dotnet test` локально, знайти тест із `Failed` |
| Check formatting | Виконати `dotnet format whitespace TripFlow.sln` і закомітити зміни |
| Start SQL Server LocalDB | Перезапустити job (**Re-run jobs**); якщо повторюється — повідомити команду |

## Правило команди

Pull request зливається лише після зелених перевірок і Approve від переглядача. У налаштуваннях репозиторію це закріплено як обов'язкову перевірку для гілки `main`.
