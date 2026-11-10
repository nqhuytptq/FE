import type { DocumentStatus, DocumentPriority } from "@/types";
import {
  getStatusColor,
  getPriorityColor,
  getStatusLabel,
  getPriorityLabel,
} from "@/lib/format";

export function StatusBadge({ status }: { status: DocumentStatus }) {
  const color = getStatusColor(status);
  return (
    <span
      className={`inline-flex items-center gap-1.5 rounded-full px-2.5 py-1 text-xs font-semibold bg-${color}-50 text-${color}-700 ring-1 ring-${color}-200`}
    >
      <span className={`h-1.5 w-1.5 rounded-full bg-${color}-500`} />
      {getStatusLabel(status)}
    </span>
  );
}

export function PriorityBadge({ priority }: { priority: DocumentPriority }) {
  const color = getPriorityColor(priority);
  return (
    <span
      className={`inline-flex items-center rounded px-2 py-0.5 text-xs font-medium bg-${color}-50 text-${color}-600`}
    >
      {getPriorityLabel(priority)}
    </span>
  );
}




