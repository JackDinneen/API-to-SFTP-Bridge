# Obi CSV Schema Specification

## Overview

All data delivered to Obi's SFTP server must conform to this 7-column CSV format. Files that do not match this schema will be rejected during ingestion.

## Required Columns

| # | Column Name    | Type    | Description                                      | Example            |
|---|---------------|---------|--------------------------------------------------|--------------------|
| 1 | Asset ID      | String  | Obi's unique identifier for the building/asset   | `AST-001`          |
| 2 | Asset name    | String  | Human-readable name of the asset                 | `Tower A - London` |
| 3 | Submeter Code | String  | Obi's identifier for the specific meter/submeter | `SM-EL-001`        |
| 4 | Utility Type  | String  | Type of utility being measured (see allowed list) | `Electricity`      |
| 5 | Year          | Integer | 4-digit year of the measurement period           | `2025`             |
| 6 | Month         | Integer | Month of the measurement period (1-12)           | `3`                |
| 7 | Value         | Numeric | Consumption value in the standard unit           | `14523.50`         |

## Accepted Utility Types and Units

| Utility Type      | Unit  | Notes                                      |
|-------------------|-------|--------------------------------------------|
| Electricity       | kWh   | Active energy consumption                  |
| Gas               | kWh   | Converted from m3 where necessary          |
| Water             | m3    | Cold and hot water combined unless split   |
| Waste             | kg    | Total waste weight                         |
| District Heating  | kWh   | Energy delivered via district heating       |
| District Cooling  | kWh   | Energy delivered via district cooling       |

## File Naming Convention

Files must follow this naming pattern:

```
[client]_[platform]_[DDMMYYYY].csv
```

- **client**: Lowercase client identifier (e.g., `acme`)
- **platform**: Lowercase source platform name (e.g., `energystar`)
- **DDMMYYYY**: Date of file generation in day-month-year format

Example: `acme_energystar_15032025.csv`

## Data Rules

1. **Monthly periods**: Each row represents one calendar month of consumption data. Do not submit daily or weekly granularity.
2. **Aggregation**: If the source provides sub-monthly data (daily, hourly), aggregate to monthly totals before generating the CSV.
3. **Unit conversion**: All values must be in the standard unit listed above. Convert at the transformation layer if the source API uses different units (e.g., convert gas from m3 to kWh using the agreed calorific value).
4. **No duplicates**: Each combination of (Asset ID + Submeter Code + Year + Month) must appear at most once per file. Duplicate rows will cause ingestion errors.
5. **No header variations**: The header row must exactly match the column names listed above, including spacing and capitalisation.
6. **Empty values**: Do not include rows with null or empty Value fields. If no data is available for a period, omit the row entirely.

## Example CSV

```csv
Asset ID,Asset name,Submeter Code,Utility Type,Year,Month,Value
AST-001,Tower A - London,SM-EL-001,Electricity,2025,1,14523.50
AST-001,Tower A - London,SM-EL-001,Electricity,2025,2,13876.25
AST-001,Tower A - London,SM-GS-001,Gas,2025,1,8234.00
AST-002,Building B - Manchester,SM-WT-001,Water,2025,1,342.75
AST-002,Building B - Manchester,SM-WS-001,Waste,2025,1,1250.00
```
