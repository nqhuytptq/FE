import { Calendar } from "lucide-react";

export interface DateRange {
  from: string;
  to: string;
}

interface DateRangeFilterProps {
  value: DateRange;
  onChange: (range: DateRange) => void;
}

const INPUT_CLASS =
  "rounded-lg border border-slate-200 px-3 py-1.5 text-sm text-slate-700 focus:border-blue-400 focus:outline-none focus:ring-2 focus:ring-blue-100";

export function DateRangeFilter({ value, onChange }: DateRangeFilterProps) {
  return (
    <div className="flex flex-wrap items-end gap-2">
      <label className="flex flex-col gap-1 text-xs font-medium text-slate-500">
        <span className="flex items-center gap-1">
          <Calendar className="h-3 w-3" />
          Ban hành từ ngày
        </span>
        <input
          type="date"
          value={value.from}
          max={value.to || undefined}
          onChange={(e) => onChange({ ...value, from: e.target.value })}
          className={INPUT_CLASS}
        />
      </label>
      <label className="flex flex-col gap-1 text-xs font-medium text-slate-500">
        <span>Đến ngày</span>
        <input
          type="date"
          value={value.to}
          min={value.from || undefined}
          onChange={(e) => onChange({ ...value, to: e.target.value })}
          className={INPUT_CLASS}
        />
      </label>
      {(value.from || value.to) && (
        <button
          onClick={() => onChange({ from: "", to: "" })}
          className="rounded-lg px-3 py-1.5 text-sm font-medium text-slate-500 transition hover:bg-slate-100"
        >
          Xóa lọc ngày
        </button>
      )}
    </div>
  );
}




