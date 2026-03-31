---
name: csv-validate
description: Validate a generated CSV file against Obi's required schema and naming convention.
disable-model-invocation: false
---

Validate the CSV against Obi's schema:

1. Check 7 required columns: Asset ID, Asset name, Submeter Code, Utility Type, Year, Month, Value
2. Validate Utility Type is one of: Electricity, Gas, Water, Waste, District Heating, District Cooling
3. Validate Year is 4-digit integer
4. Validate Month is integer 1-12
5. Validate Value is numeric
6. Check file naming matches [client]_[platform]_[DDMMYYYY].csv
7. Check no duplicate (Asset ID + Submeter Code + Year + Month) combinations
8. Report: pass/fail per row with error details
