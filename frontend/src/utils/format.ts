export function formatDate(value: string) {
  return new Intl.DateTimeFormat('en-GB', { day: '2-digit', month: 'short', year: 'numeric' }).format(new Date(value));
}

export function formatDateTime(value: string) {
  return new Intl.DateTimeFormat('en-GB', {
    day: '2-digit',
    month: 'short',
    hour: '2-digit',
    minute: '2-digit'
  }).format(new Date(value));
}

export function formatMonth(value: string) {
  return new Intl.DateTimeFormat('en-GB', { month: 'short', year: '2-digit' }).format(new Date(`${value}-01T00:00:00`));
}

export function formatNumber(value: number) {
  return value.toLocaleString();
}
