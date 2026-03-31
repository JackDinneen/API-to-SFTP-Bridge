export interface Connection {
  id: string
  name: string
  baseUrl: string
  authType: AuthType
  status: ConnectionStatus
  scheduleCron: string
  clientName: string
  platformName: string
  sftpHost: string
  sftpPort: number
  sftpPath: string
  reportingLagDays: number
  createdBy: string
  createdAt: string
  updatedAt: string
}

export enum ConnectionStatus {
  Active = 'Active',
  Paused = 'Paused',
  Error = 'Error',
}

export enum AuthType {
  ApiKey = 'ApiKey',
  OAuth2ClientCredentials = 'OAuth2ClientCredentials',
  BasicAuth = 'BasicAuth',
  CustomHeaders = 'CustomHeaders',
}

export enum UserRole {
  Admin = 'Admin',
  Operator = 'Operator',
  Viewer = 'Viewer',
}

export enum SyncRunStatus {
  Pending = 'Pending',
  Running = 'Running',
  Succeeded = 'Succeeded',
  Failed = 'Failed',
}

export interface SyncRun {
  id: string
  connectionId: string
  status: SyncRunStatus
  startedAt: string
  completedAt?: string
  recordCount: number
  fileSize: number
  fileName: string
  errorMessage?: string
  triggeredBy: string
}

export interface ConnectionMapping {
  id: string
  connectionId: string
  sourcePath: string
  targetColumn: string
  transformType: string
  transformConfig: Record<string, unknown>
  sortOrder: number
}

export interface ApiResponse<T> {
  success: boolean
  data?: T
  message?: string
  errors?: string[]
}
