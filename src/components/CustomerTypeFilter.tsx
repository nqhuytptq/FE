import type { CustomerType } from "@/types";
import { CUSTOMER_TYPE_LABELS } from "@/types";

export type CustomerTypeFilterValue = CustomerType | "all";

interface CustomerTypeFilterProps {
  value: CustomerTypeFilterValue;
  onChange: (value: CustomerTypeFilterValue) => void;
}

const OPTIONS: { key: CustomerTypeFilterValue; label: string }[] = [
  { key: "all", label: "Tất cả loại KH" },
  { key: "company", label: CUSTOMER_TYPE_LABELS.company },
  { key: "individual", label: CUSTOMER_TYPE_LABELS.individual },
];

export function CustomerTypeFilter({ value, onChange }: CustomerTypeFilterProps) {
  return (
    <div className="flex flex-col gap-1">
      <span className="text-xs font-medium text-slate-500">Loại khách hàng</span>
      <div className="flex rounded-lg bg-slate-100 p-0.5">
        {OPTIONS.map((opt) => (
          <button
            key={opt.key}
            type="button"
            onClick={() => onChange(opt.key)}
            className={`rounded-md px-3 py-1.5 text-sm font-medium transition ${
              value === opt.key ? "bg-white text-slate-900 shadow-sm" : "text-slate-500"
            }`}
          >
            {opt.label}
          </button>
        ))}
      </div>
    </div>
  );
}




