export type UserRole = 'Admin' | 'Reseller';
export type AlertSeverity = 'Info' | 'Warning' | 'Critical';
export type AlertStatus = 'Open' | 'Acknowledged' | 'Notified' | 'Resolved' | 'Dispatched';
export type PartType = 'Motor' | 'Belt' | 'Relay' | 'Sensor';
export type ControllerKind = 'Plc' | 'Pcb';

export interface LoginResponse {
  token: string;
  expiresAt: string;
  user: UserProfile;
}

export interface UserProfile {
  id: string;
  displayName: string;
  email: string;
  role: UserRole;
  companyId?: string;
  companyName?: string;
}

export interface DashboardSummary {
  companyCount: number;
  machineCount: number;
  partCount: number;
  openAlertCount: number;
  criticalAlertCount: number;
  resellersAtRisk: ResellerRisk[];
  recentAlerts: Alert[];
}

export interface ResellerRisk {
  companyId: string;
  companyName: string;
  openAlerts: number;
  criticalAlerts: number;
  earliestDueDate: string;
  averageAlertHealthScore: number;
  fleetHealthScore: number;
}

export interface Company {
  id: string;
  name: string;
  contactName: string;
  contactEmail: string;
  industry: string;
  machineCount: number;
  openAlertCount: number;
  fleetHealthScore: number;
  machines: Machine[];
}

export interface Machine {
  id: string;
  companyId: string;
  companyName: string;
  name: string;
  machineType: string;
  location: string;
  installedOn: string;
  controller: ControllerUnit;
  openAlertCount: number;
  healthScore: number;
}

export interface ControllerUnit {
  id: string;
  kind: ControllerKind;
  manufacturer: string;
  model: string;
  serialNumber: string;
  firmwareVersion: string;
  parts: Part[];
}

export interface Part {
  id: string;
  name: string;
  type: PartType;
  manufacturer: string;
  model: string;
  installedOn: string;
  lastReplacedOn?: string;
  replacementCost: number;
  totalMovements: number;
  totalRotations: number;
  averageTemperatureC: number;
  prediction: Prediction;
}

export interface Prediction {
  healthScore: number;
  predictedDueDate: string;
  severity: AlertSeverity;
  confidence: number;
  reasonCodes: string[];
  movementRatio: number;
  ageRatio: number;
  temperatureRatio: number;
  expectedMovementsToReplacement: number;
  expectedDaysToReplacement: number;
  expectedTemperatureAtReplacementC: number;
}

export interface Alert {
  id: string;
  companyId: string;
  companyName: string;
  machineId: string;
  machineName: string;
  partId: string;
  partName: string;
  partType: PartType;
  severity: AlertSeverity;
  status: AlertStatus;
  createdAt: string;
  predictedDueDate: string;
  healthScore: number;
  message: string;
  notificationChannel?: string;
  notifiedAt?: string;
}

export interface PartLifetime {
  partType: PartType;
  replacementSamples: number;
  averageMovementsToReplacement: number;
  averageDaysToReplacement: number;
  averageTemperatureAtReplacementC: number;
  longestLastingPart: string;
  models: PartModelLifetime[];
}

export interface PartModelLifetime {
  manufacturer: string;
  model: string;
  partName: string;
  replacementSamples: number;
  averageMovementsToReplacement: number;
  averageDaysToReplacement: number;
  averageTemperatureAtReplacementC: number;
  bestDaysToReplacement: number;
  lowestDaysToReplacement: number;
}

export interface FailureTrend {
  month: string;
  inspections: number;
  repairs: number;
  replacements: number;
  failureReports: number;
  totalRecords: number;
}

export interface SupportSettings {
  companyId: string;
  companyName: string;
  supportDepartmentEmail: string;
  technicianDispatchEmail: string;
  escalationPhone: string;
  defaultSlaHours: number;
  updatedAt: string;
}

export interface DispatchTechnicianResult {
  machineId: string;
  alertId?: string;
  recipients: string[];
  sentAt: string;
}

export interface FaqPrompt {
  label: string;
  prompt: string;
}

export interface AssistantChatMessage {
  id: string;
  role: 'user' | 'assistant' | 'system';
  content: string;
  createdAt: string;
}

export interface AssistantSession {
  id: string;
  title: string;
  createdAt: string;
  updatedAt: string;
  messages: AssistantChatMessage[];
}
