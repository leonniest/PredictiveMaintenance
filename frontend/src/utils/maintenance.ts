import type { Alert, Machine, Part } from '../types';

export function severityClass(value: string) {
  return value.toLowerCase();
}

export function healthSeverity(health: number) {
  if (health <= 35) return 'critical';
  if (health <= 65) return 'warning';
  return 'info';
}

export function machineAlertCount(machine: Machine, alerts: Alert[]) {
  return alerts.filter((alert) => alert.machineId === machine.id && isOpenAlert(alert)).length;
}

export function isOpenAlert(alert: Alert) {
  return alert.status === 'Open' || alert.status === 'Notified';
}

export function usageLabel(part: Part) {
  return part.type === 'Motor' || part.type === 'Belt' ? 'rotations' : 'activations';
}
