# Wukong Benchmark Runner

Консольный инструмент на C# для локального автоматизированного запуска **Black Myth: Wukong Benchmark Tool** через Steam. Он последовательно выполняет CPU-oriented и GPU-oriented проходы, собирает сведения о компьютере, разбирает результаты Benchmark Tool и сохраняет единый JSON-отчёт.

> Инструмент работает в Windows и требует установленный через Steam Black Myth: Wukong Benchmark Tool, а также активную авторизацию в Steam.

## Возможности

- Обнаруживает Steam и каталог Benchmark Tool в библиотеках Steam.
- Создаёт резервную копию `GameUserSettings.ini` и восстанавливает исходный файл после завершения либо ошибки.
- Применяет два набора Unreal Engine-настроек и запускает Benchmark Tool через Steam AppID `3132990`.
- Ожидает завершения процесса Benchmark Tool.
- Собирает CPU, GPU, объём RAM, ОС и количество логических процессоров.
- Ищет свежий файл результата и извлекает `Average FPS`, `Minimum FPS`, `Maximum FPS` и `Score`.
- Формирует форматированный JSON-отчёт с настройками и результатами обоих проходов.

## Требования

- Windows.
- [.NET SDK 10](https://dotnet.microsoft.com/download).
- Установленный **Black Myth: Wukong Benchmark Tool** в Steam.
- Запущенный и авторизованный Steam-клиент.

## Запуск

Клонируйте репозиторий и перейдите в его каталог:

```powershell
git clone https://github.com/bankhaev01-coder/wukong-benchmark-runner.git
Set-Location .\wukong-benchmark-runner
```

Проверьте сборку и модульные тесты:

```powershell
dotnet test .\WukongBenchmarkRunner.sln
```

Проверьте обнаружение Steam и Benchmark Tool:

```powershell
dotnet run --project .\WukongBenchmarkRunner -- detect
```

Запустите основной сценарий из двух проходов:

```powershell
dotnet run --project .\WukongBenchmarkRunner -- run --timeout-minutes 30 --output .\wukong-report.json
```

После выполнения будет выведен путь к JSON-отчёту. В нём указаны сведения о машине, использованные настройки, время каждого прохода и доступные метрики Benchmark Tool.

### Явное указание путей

Если Steam или конфигурация не обнаруживаются автоматически, укажите их вручную:

```powershell
dotnet run --project .\WukongBenchmarkRunner -- run `
  --steam-path "C:\Program Files (x86)\Steam\steam.exe" `
  --config "<путь-к-GameUserSettings.ini>" `
  --timeout-minutes 30 `
  --output .\wukong-report.json
```

Дополнительные команды:

```powershell
# Вывести сведения о машине, установке и профилях без запуска Benchmark Tool.
dotnet run --project .\WukongBenchmarkRunner -- dry-run

# Разобрать уже существующие результаты из указанного каталога.
dotnet run --project .\WukongBenchmarkRunner -- parse --results-path "C:\Temp\results" --output .\parsed-result.json
```

## Выбор настроек

### CPU-oriented проход

Цель CPU-профиля — уменьшить вероятность ограничения производительности видеокартой, чтобы FPS в большей степени зависел от подготовки сцены и процессора. Для этого профиль устанавливает разрешение `1280×720`, `50%` resolution scale и screen percentage, отключает Ray Tracing и снижает до минимальных уровней Anti-Aliasing, Shadows, Global Illumination, Reflections, Post Processing, Textures, Effects и Foliage. При этом `ViewDistanceQuality=3` сохраняет заметную нагрузку, связанную с обработкой сцены и дальностью видимости.

Игровой benchmark всегда создаёт смешанную CPU/GPU-нагрузку, поэтому это не изолированный лабораторный CPU-тест, а CPU-oriented сценарий, снижающий вклад GPU.

### GPU-oriented проход

Цель GPU-профиля — максимально увеличить сложность графического пайплайна при фиксированном разрешении, используемом Benchmark Tool. Для этого задаются максимальные уровни `4` для View Distance, Anti-Aliasing, Shadows, Global Illumination, Reflections, Post Processing, Textures, Effects и Foliage, включаются Ray Tracing и Dynamic Global Illumination, а `ResolutionQuality` и `ScreenPercentage` устанавливаются в `100%`.

Результат зависит от поддержки Ray Tracing драйвером и видеокартой. Реальные имена параметров Unreal Engine и формат выходных файлов следует подтвердить на установленной версии Benchmark Tool.

## Проверка качества

В репозитории есть модульные тесты для изменения и восстановления Unreal INI, а также разбора метрик результата. На момент публикации выполнено:

```text
dotnet test .\WukongBenchmarkRunner.sln --no-restore --verbosity minimal

Passed: 4
Failed: 0
```

Полный запуск Benchmark Tool требует локально установленного приложения Steam; в среде подготовки репозитория оно не было доступно, поэтому end-to-end проверку Steam-запуска и фактического формата результатов необходимо выполнить на целевой машине.

## Структура

- `WukongBenchmarkRunner` — CLI-приложение.
- `WukongBenchmarkRunner.Tests` — тесты MSTest.
- `WukongBenchmarkRunner.sln` — solution-файл.
