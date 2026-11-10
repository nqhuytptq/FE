import type {
  DocumentStatus,
  DocumentPriority,
  CirculationAction,
} from "@/types";
import {
  STATUS_LABELS,
  PRIORITY_LABELS,
  ACTION_LABELS,
} from "@/types";

/** Múi giờ chuẩn: GMT+07:00 (Bangkok, Hà Nội, TP. Hồ Chí Minh), không DST. */
export const DISPLAY_TIME_ZONE = "Asia/Bangkok";

export function formatCurrency(amount: number): string {
  return new Intl.NumberFormat("vi-VN", {
    style: "currency",
    currency: "VND",
    maximumFractionDigits: 0,
  }).format(amount);
}

/**
 * Backend lưu UTC dưới dạng ISO không có 'Z' ở đuôi (tùy serializer),
 * cần bổ sung 'Z' nếu thiếu để JavaScript không parse nhầm theo giờ máy cục bộ.
 */
function parseApiDate(dateStr: string): Date {
  if (!dateStr) return new Date();
  const trimmed = dateStr.trim();
  // Nếu là dạng chỉ có ngày YYYY-MM-DD
  if (/^\d{4}-\d{2}-\d{2}$/.test(trimmed)) {
    const [y, m, d] = trimmed.split("-").map(Number);
    return new Date(y, m - 1, d, 12, 0, 0);
  }
  const hasTz = /(?:Z|[+-]\d{2}:?\d{2})$/i.test(trimmed);
  return new Date(hasTz ? trimmed : `${trimmed}Z`);
}

export function formatDate(dateStr: string): string {
  if (!dateStr) return "—";
  return parseApiDate(dateStr).toLocaleDateString("vi-VN", {
    timeZone: DISPLAY_TIME_ZONE,
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
}

export function formatDateTime(dateStr: string): string {
  if (!dateStr) return "—";
  return parseApiDate(dateStr).toLocaleString("vi-VN", {
    timeZone: DISPLAY_TIME_ZONE,
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false,
  });
}

/** Lấy ngày hôm nay theo chuẩn múi giờ GMT+7 (Bangkok/Hà Nội) dạng YYYY-MM-DD cho input date */
export function getTodayDateGmt7(): string {
  const parts = new Intl.DateTimeFormat("en-CA", {
    timeZone: DISPLAY_TIME_ZONE,
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
  }).formatToParts(new Date());
  const values = Object.fromEntries(parts.map((p) => [p.type, p.value]));
  return `${values.year}-${values.month}-${values.day}`;
}

export function getStatusColor(status: DocumentStatus): string {
  switch (status) {
    case "Draft":
      return "slate";
    case "PkhPendingSup":
      return "amber";
    case "PqlnPendingReceive":
      return "sky";
    case "PqlnReceived":
      return "indigo";
    case "PqlnPendingSup":
      return "amber";
    case "PdvPendingReceive":
      return "sky";
    case "PdvReceived":
      return "indigo";
    case "PdvEntryPosted":
      return "orange";
    case "PdvEntryApproved":
      return "amber";
    case "Completed":
      return "emerald";
    case "Rejected":
      return "rose";
    default:
      return "slate";
  }
}

export function getPriorityColor(priority: DocumentPriority): string {
  switch (priority) {
    case "low":
      return "sky";
    case "normal":
      return "slate";
    case "high":
      return "orange";
    case "urgent":
      return "red";
  }
}

export function getActionColor(action: CirculationAction): string {
  switch (action) {
    case "Created":
      return "sky";
    case "PkhSupApproved":
      return "emerald";
    case "PqlnReceived":
      return "indigo";
    case "PqlnSubmittedSup":
      return "amber";
    case "PqlnSupApproved":
      return "emerald";
    case "PdvReceived":
      return "indigo";
    case "PdvEntryPosted":
      return "orange";
    case "PdvSupApprovedEntry":
      return "amber";
    case "Supplemented":
      return "blue";
    case "Completed":
      return "emerald";
    case "Rejected":
      return "rose";
    case "Returned":
      return "amber";
    default:
      return "slate";
  }
}

export function getStatusLabel(status: DocumentStatus): string {
  return STATUS_LABELS[status] || status;
}

export function getPriorityLabel(priority: DocumentPriority): string {
  return PRIORITY_LABELS[priority];
}

export function getActionLabel(action: CirculationAction): string {
  return ACTION_LABELS[action] || action;
}

function parseDateOnly(dateStr: string): Date {
  const [year, month, day] = dateStr.split("-").map(Number);
  return new Date(year, month - 1, day);
}

export function daysUntilDue(dueDate: string): number {
  const today = new Date();
  today.setHours(0, 0, 0, 0);
  const due = parseDateOnly(dueDate);
  return Math.round((due.getTime() - today.getTime()) / (1000 * 60 * 60 * 24));
}

export function getEffectiveStatus(doc: { status: DocumentStatus; due_date: string }): DocumentStatus {
  return doc.status;
}



