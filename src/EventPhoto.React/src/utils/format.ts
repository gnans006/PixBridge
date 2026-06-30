export function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('en-US', {
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  });
}

export function formatDateTime(dateStr: string): string {
  return new Date(dateStr).toLocaleString('en-US', {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  });
}

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  if (bytes < 1_048_576) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }

  if (bytes < 1_073_741_824) {
    return `${(bytes / 1_048_576).toFixed(1)} MB`;
  }

  return `${(bytes / 1_073_741_824).toFixed(2)} GB`;
}
