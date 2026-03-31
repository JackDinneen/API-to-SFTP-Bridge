# Product Requirements Document: API-to-SFTP Bridge

**Product Name:** Obi Bridge
**Version:** 1.0 (MVP)
**Author:** Jack Dinneen
**Date:** 31 March 2026
**Status:** Draft

---

## 1. Problem Statement

Obi's ESG platform does not expose a public API. The only automated data ingestion path is via SFTP upload of CSV files conforming to a strict schema. Today, onboarding a new external data provider (e.g., an energy management platform serving a real estate fund) requires:

1. Sharing a CSV template and a written specification document with the vendor.
2. The vendor manually extracting data from their own system, transforming it to match Obi's schema, and uploading it.
3. Back-and-forth when field names don't align, units are wrong, asset IDs don't match, or meters change.

This process is slow, error-prone, and doesn't scale. Every new client-platform combination requires bespoke coordination.

**Obi Bridge** solves this by sitting between any external REST API and Obi's SFTP endpoint — automatically pulling data, transforming it via user-configured (AI-assisted) mappings, validating it, and delivering a correctly formatted CSV to Obi's SFTP server on a configurable schedule.

---

## 2. Vision & Objectives

**Vision:** A single, intuitive platform that lets anyone connect an external API to Obi's SFTP ingestion pipeline in minutes — no code, no CSV wrangling, no emails back and forth.

**Objectives:**

- Reduce integration setup time from weeks to hours.
- Eliminate manual CSV preparation and the errors it introduces.
- Support any REST API as a data source with full auth flexibility.
- Provide AI-assisted field mapping so users don't need to understand both schemas.
- Enable configurable scheduling so data flows automatically at the right cadence.
- Build a foundation that scales to dozens of client-platform combinations without additional engineering effort.

---

## 3. Users & Access Model

### 3.1 User Roles

| Role | Description | Permissions |
|------|-------------|-------------|
| **Admin** (Obi internal) | Obi team members who manage the platform | Full access: create/edit/delete connections, manage users, configure SFTP targets, view all audit logs, trigger Obi validation |
| **Operator** (External vendor) | Representatives from data providers (e.g., GARBE, Deepki, Measurabl) | View and configure connections assigned to them, trigger manual syncs, view their own sync history and error logs |
| **Viewer** (Client stakeholder) | Client-side users (e.g., BlackRock ESG team) who want visibility | Read-only: view sync status, download generated CSVs, view data quality reports |

### 3.2 Authentication

Platform login via email/password with SSO (Google, Microsoft) support. Role assignment managed by Admins.

---

## 4. Core Architecture

```
┌─────────────────┐     ┌──────────────────────────────────┐     ┌─────────────┐
│  External API    │────▶│         OBI BRIDGE                │────▶│  Obi SFTP   │
│  (any REST API)  │     │                                  │     │  Server     │
└─────────────────┘     │  ┌────────┐  ┌────────────────┐  │     └─────────────┘
                        │  │API      │  │ Transform &    │  │
                        │  │Connector│─▶│ Map Engine     │──┤
                        │  └────────┘  └────────────────┘  │
                        │       │            │              │
                        │  ┌────▼───┐  ┌─────▼──────────┐  │
                        │  │Auth    │  │ Validation &   │  │
                        │  │Vault   │  │ Reconciliation │  │
                        │  └────────┘  └────────────────┘  │
                        │       │            │              │
                        │  ┌────▼────────────▼───────────┐  │
                        │  │  Scheduler & Orchestrator   │  │
                        │  └─────────────────────────────┘  │
                        │  ┌─────────────────────────────┐  │
                        │  │  Audit Log & Notifications  │  │
                        │  └─────────────────────────────┘  │
                        └──────────────────────────────────┘
```

### 4.1 Key Components

**API Connector Layer** — Generic HTTP client that connects to any REST API. Supports paginated responses, rate limiting, retry logic, and response caching.

**Auth Vault** — Secure credential storage for external API authentication. Supports API Key/Bearer Token, OAuth 2.0 (Authorization Code, Client Credentials), Basic Auth, and custom header injection. Credentials encrypted at rest (AES-256) and never exposed in logs or UI.

**Transform & Map Engine** — The core of the product. Takes raw API response data and transforms it into Obi's CSV schema using user-configured mappings. Includes AI-assisted mapping suggestions, user-configurable unit conversions, value transformations (e.g., date formatting, string concatenation), and static value injection (e.g., hardcoded Asset IDs for known assets).

**Validation & Reconciliation** — Optional, user-triggered validation layer that checks generated data against Obi's known records. Not a blocking gate — syncs can proceed without validation.

**Scheduler & Orchestrator** — Manages sync timing per connection (daily, weekly, monthly, on-demand). Handles retries on failure, dependency ordering, and parallel execution.

**SFTP Delivery** — Generates the CSV file, names it per convention, and pushes it to Obi's existing SFTP server via SFTP/SSH.

**Audit Log & Notifications** — Full history of every sync, transformation, and delivery. Email/webhook notifications on success, failure, or data quality warnings.

---

## 5. Obi CSV Output Schema

Every generated CSV must conform exactly to the following structure:

| Column | Type | Description | Example |
|--------|------|-------------|---------|
| `Asset ID` | String | Unique asset identifier matching Obi's records | `EF001` |
| `Asset name` | String | Human-readable asset name matching Obi | `Building 1` |
| `Submeter Code` | String | Meter/submeter identifier matching Obi | `ELEC01` |
| `Utility Type` | Enum | One of the accepted utility types (see §5.1) | `Electricity` |
| `Year` | Integer | 4-digit year of the reporting period | `2024` |
| `Month` | Integer | Month number (1–12) of the reporting period | `12` |
| `Value` | Numeric | Consumption value in the required unit | `8000` |

### 5.1 Accepted Utility Types & Units

| Utility Type | Required Unit |
|-------------|---------------|
| Electricity | kWh |
| Gas | kWh |
| Water | m³ |
| Waste | t (tonnes) |
| District Heating | kWh |
| District Cooling | kWh |

Additional utility types can be added by an Admin via platform configuration.

### 5.2 File Naming Convention

```
[client]_[platform]_[date].csv
```

- **client**: Lowercase, underscored client name (e.g., `blackrock`)
- **platform**: Uppercase or mixed-case platform name (e.g., `GARBE`)
- **date**: Date in `DDMMYYYY` format (e.g., `08052025`)

Example: `blackrock_GARBE_08052025.csv`

### 5.3 Data Rules

- Data must represent **monthly** periods from the **1st to the last day of the month** inclusive.
- Only meters with a **corresponding record in Obi** should be included.
- If a source API provides data at sub-monthly granularity (e.g., daily or 15-minute intervals), the platform must **aggregate to monthly totals**.
- If a source API provides data in units other than those required, the platform must **apply the user-configured conversion**.

---

## 6. Feature Specification

### 6.1 Connection Setup Wizard

A step-by-step guided flow for creating a new API connection:

**Step 1 — Source API Configuration**
- Enter base URL of the external API.
- Select authentication method (API Key, OAuth 2.0, Basic Auth, Custom Headers).
- Enter credentials (stored in Auth Vault).
- Test connection — platform makes a simple authenticated request and confirms connectivity.

**Step 2 — Endpoint Discovery**
- User specifies or browses API endpoints (e.g., `/v1/buildings/{id}/consumption`).
- Platform makes a sample request and displays the response structure as a navigable JSON tree.
- User selects the endpoint(s) that contain the data they need.
- For paginated endpoints, user configures pagination strategy (offset, cursor, page number).

**Step 3 — AI-Assisted Field Mapping**
- Platform displays the source API's response fields on the left, and Obi's seven required CSV columns on the right.
- **AI engine analyses field names, data types, and sample values** to suggest mappings (e.g., `building_code` → `Asset ID`, `energy_kwh` → `Value`).
- User reviews, accepts, or overrides each suggestion.
- For fields requiring transformation, user configures:
  - **Value mapping** (e.g., source `"electric"` → Obi `"Electricity"`).
  - **Unit conversion** (e.g., source is MWh → multiply by 1000 → output kWh).
  - **Date parsing** (e.g., source sends `"2024-12-01T00:00:00Z"` → extract Year: `2024`, Month: `12`).
  - **Static values** (e.g., always set `Asset ID` to `EF001` for this endpoint).
  - **Concatenation / splitting** (e.g., source `"EF001-ELEC01"` → split on `-` → `Asset ID` + `Submeter Code`).

**Step 4 — Aggregation Rules**
- If source data is sub-monthly: configure aggregation (sum, average, last reading, max).
- If source data is already monthly: confirm and pass through.

**Step 5 — Output Configuration**
- Set the **client name** and **platform name** for file naming.
- Configure the target SFTP connection (host, port, path, credentials).
- Set the **sync schedule** (cron expression or simple picker: daily, weekly, monthly, specific day of month).
- Set the **reporting lag** (e.g., "sync on the 5th of each month for the previous month's data").

**Step 6 — Test & Activate**
- Run a test sync for a single month of data.
- Preview the generated CSV in the browser before delivery.
- If validation is enabled, show validation results against Obi records.
- Activate the connection to begin scheduled syncs.

### 6.2 AI-Assisted Mapping Engine

The mapping engine is the differentiating feature of the platform. It should:

- **Analyse field names** using NLP (e.g., `kwh_consumption` clearly maps to `Value` with `Utility Type: Electricity`).
- **Inspect sample data** to infer types and formats (e.g., `"2024-12"` is a year-month string).
- **Learn from previous mappings** — if the same API has been connected by another user, suggest the same proven mappings.
- **Provide a confidence score** for each suggestion (High / Medium / Low) so users know where to focus attention.
- **Handle nested JSON** — allow users to map from deeply nested paths (e.g., `response.data[0].meters[].readings.value`).

### 6.3 Unit Conversion Engine

A configurable conversion layer that users set per field mapping:

| From | To | Formula |
|------|----|---------|
| MWh | kWh | × 1,000 |
| GJ | kWh | × 277.778 |
| therms | kWh | × 29.3071 |
| gallons (US) | m³ | × 0.00378541 |
| litres | m³ | ÷ 1,000 |
| kg | t | ÷ 1,000 |
| lbs | t | ÷ 2,204.62 |
| cubic feet | m³ | × 0.0283168 |

The platform ships with common conversions pre-loaded. Admins can add custom conversion formulas.

### 6.4 Optional Obi Validation

A user-triggered (not automatic) validation step:

- **Asset ID validation** — check that every Asset ID in the generated CSV exists in Obi's known records.
- **Submeter Code validation** — check that every Submeter Code exists and is associated with the correct Asset ID.
- **Utility Type validation** — confirm utility types are in the accepted list.
- **Duplicate detection** — flag if the same Asset ID + Submeter Code + Year + Month combination appears more than once.
- **Range checks** — flag values that are statistical outliers compared to historical data for the same meter (e.g., 10× the typical monthly value).

Validation is powered by a **reference data file** (CSV or JSON) uploaded by an Admin containing Obi's known Asset IDs, Asset Names, Submeter Codes, and their associated Utility Types. This avoids the need for a direct Obi API (which doesn't exist) while still enabling cross-referencing.

Validation results are presented as a report:
- ✅ **Passed** — record matches Obi's reference data.
- ⚠️ **Warning** — value looks unusual (outlier) but record structure is valid.
- ❌ **Error** — Asset ID or Submeter Code not found in Obi's reference data.

**Critically: validation does not block delivery.** Users can review and proceed.

### 6.5 Sync Dashboard

The main operational view for all users:

- **Connection list** — all configured connections with status (Active, Paused, Error).
- **Last sync** — timestamp, record count, file size, delivery status.
- **Next sync** — scheduled time.
- **Quick actions** — Trigger manual sync, Pause, View logs, Download last CSV.
- **Health indicators** — green/amber/red per connection based on recent sync success rate.

### 6.6 Audit Trail

Every action is logged:

- Connection created/modified/deleted (by whom, when).
- Sync triggered (manual or scheduled).
- API data fetched (endpoint, response size, duration).
- Transformation applied (records in, records out, any warnings).
- Validation run (results summary).
- CSV generated (filename, record count, hash).
- SFTP delivery (target, success/failure, retry count).

Logs are retained for 12 months and exportable as CSV.

### 6.7 Notifications

Configurable per connection:

- **On success**: Email/webhook confirmation with record count and filename.
- **On failure**: Email/webhook alert with error details and suggested resolution.
- **On validation warning**: Email/webhook with summary of flagged records.
- **On meter change detected**: Alert when the source API returns meters not seen in previous syncs (addresses the requirement to notify before including new meters).

---

## 7. User Flows

### 7.1 New Integration Setup (Admin)

```
Admin logs in
  → Clicks "New Connection"
  → Enters source API details & credentials
  → Tests connection ✓
  → Selects data endpoint
  → AI suggests field mappings
  → Admin reviews/adjusts mappings
  → Configures unit conversions where needed
  → Sets file naming (client + platform)
  → Configures SFTP target
  → Sets schedule (e.g., 5th of each month)
  → Runs test sync for Dec 2024
  → Previews CSV → looks correct
  → Optionally runs Obi validation
  → Activates connection
  → Assigns Operator access to vendor contact
```

### 7.2 Monthly Sync (Automated)

```
Scheduler triggers on configured date
  → API Connector authenticates with source
  → Fetches data for the target month
  → Transform engine applies mappings
  → Unit conversions applied
  → Monthly aggregation if needed
  → CSV generated with correct filename
  → (If enabled) Validation run against reference data
  → CSV pushed to Obi SFTP
  → Audit log entry written
  → Notification sent to configured recipients
```

### 7.3 Vendor Monitors Their Connection (Operator)

```
Operator logs in
  → Sees their assigned connections
  → Views sync history for their connection
  → Sees last sync succeeded with 247 records
  → Downloads the CSV to verify
  → Notices a new meter was added on their side
  → Contacts Obi Admin to update reference data before next sync
```

---

## 8. Non-Functional Requirements

| Requirement | Target |
|-------------|--------|
| **Availability** | 99.5% uptime (syncs are not real-time critical) |
| **Latency** | Sync completion < 5 minutes for up to 10,000 records |
| **Scalability** | Support 100+ active connections concurrently |
| **Security** | All credentials encrypted at rest (AES-256) and in transit (TLS 1.2+). SOC 2 Type II compliance roadmap. |
| **Data retention** | Generated CSVs retained for 90 days. Audit logs for 12 months. |
| **Credential isolation** | Operator users can never see API keys or SFTP passwords — only Admins can manage credentials |
| **Rate limiting** | Respect external API rate limits with configurable throttling per connection |
| **Retry logic** | Exponential backoff with max 3 retries on transient failures |
| **Idempotency** | Re-running a sync for the same period overwrites (not duplicates) the previous file |

---

## 9. Technical Recommendations (MVP)

| Layer | Recommendation | Rationale |
|-------|----------------|-----------|
| **Frontend** | React + Tailwind (Next.js) | Fast to build, component-rich ecosystem for the mapping UI |
| **Backend** | Node.js (Express or Fastify) or Python (FastAPI) | Both handle HTTP orchestration well; Python has stronger data transformation libraries |
| **Database** | PostgreSQL (via Supabase) | Connection configs, mapping definitions, audit logs, user management — all relational. Supabase gives auth + RLS out of the box |
| **Scheduler** | pg_cron (Supabase) or BullMQ (Redis) | Simple cron scheduling with job queue for reliability |
| **AI Mapping** | Claude API (Anthropic) | Use Claude to analyse source schema + sample data and suggest Obi field mappings with confidence scores |
| **Secrets** | Supabase Vault or AWS Secrets Manager | Encrypted credential storage |
| **SFTP Client** | `ssh2-sftp-client` (Node) or `paramiko` (Python) | Mature, well-tested SFTP libraries |
| **Hosting** | Vercel (frontend) + Supabase (backend + DB) or Railway | Low-ops, scales automatically |

---

## 10. MVP Scope vs. Future

### MVP (v1.0)

- Connection setup wizard with manual + AI-assisted mapping.
- Support for REST APIs with JSON responses.
- Full auth flexibility (API Key, OAuth 2.0, Basic Auth, Custom Headers).
- Unit conversion engine with pre-loaded common conversions.
- Monthly aggregation from sub-monthly data.
- CSV generation conforming to Obi's schema and naming convention.
- Push to a single Obi SFTP server.
- Configurable sync scheduling.
- Sync dashboard with status and history.
- Optional validation against uploaded reference data.
- Email notifications on sync success/failure.
- Admin, Operator, and Viewer roles.
- Full audit logging.

### v1.1

- Webhook notifications (Slack, Teams, custom).
- Bulk connection cloning (set up one connection, duplicate for similar clients).
- Mapping templates — save and reuse mappings for the same source platform across clients.
- CSV preview diff — compare this month's output to last month's to catch anomalies.

### v2.0

- Direct Obi API integration when available (replacing SFTP delivery).
- Support for non-REST sources (GraphQL, SOAP, flat file ingestion).
- Two-way sync — push data back to source platforms.
- Multi-SFTP target support (different SFTP servers per client).
- Advanced analytics dashboard — trends, data quality scores over time.
- Self-service vendor onboarding portal.

---

## 11. Success Metrics

| Metric | Target (6 months post-launch) |
|--------|-------------------------------|
| Integration setup time | < 2 hours (down from 2–4 weeks) |
| Sync success rate | > 98% |
| Manual CSV preparation | Eliminated for all connected sources |
| Active connections | 20+ |
| Data quality errors | < 1% of records per sync |
| User satisfaction (NPS) | > 40 |

---

## 12. Risks & Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| External API changes without notice | Sync failures, incorrect data | Schema change detection: compare response structure to saved schema on each sync; alert on drift |
| Vendor API rate limits too restrictive | Slow or failed data pulls | Configurable throttling, caching, and off-peak scheduling |
| AI mapping suggests incorrect field | Wrong data in CSV | All AI suggestions require human confirmation; confidence scoring highlights uncertain mappings |
| Obi Asset ID / Submeter Code mismatches | Rejected or orphaned data in Obi | Optional validation layer + new meter detection alerts |
| SFTP delivery failures | Data not reaching Obi | Retry logic + manual re-trigger + file download as fallback |
| Credential security breach | Unauthorised API access | Encrypted vault, role-based access, audit logging, credential rotation reminders |

---

## 13. Open Questions

1. **Obi reference data format** — What's the easiest way to export Obi's current Asset IDs, Submeter Codes, and Utility Types for the validation layer? Manual CSV upload is the MVP, but is there a database export or report we can automate?

2. **File delivery confirmation** — Does Obi's SFTP ingestion process provide any acknowledgment (e.g., a `.done` file, email, or status endpoint) that the file was processed successfully? If so, we can close the loop in the audit trail.

3. **Historical backfill** — When onboarding a new connection, should the platform support pulling historical data (e.g., the last 12 months) in a single batch, or only start from the current period forward?

4. **Multi-tenancy model** — Should each client (e.g., BlackRock, Hines) see a completely isolated instance, or is a shared platform with data segregation via RLS sufficient?

5. **Billing model** — Is this an internal tool (cost centre) or will it be offered to clients/vendors as a paid add-on?

---

*End of PRD — Obi Bridge v1.0*
