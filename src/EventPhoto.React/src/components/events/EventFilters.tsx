import type { EventResponse } from '../../types';

interface EventFiltersProps {
  activeFilter: string;
  onFilterChange: (filter: string) => void;
  eventTypes: string[];
  events: EventResponse[];
}

interface Chip {
  id: string;
  label: string;
  count: number;
}

export function EventFilters({ activeFilter, onFilterChange, eventTypes, events }: EventFiltersProps) {
  const chips: Chip[] = [
    { id: 'all',         label: 'All',         count: events.length },
    { id: 'active',      label: 'Active',       count: events.filter(e => e.isActive).length },
    { id: 'inactive',    label: 'Inactive',     count: events.filter(e => !e.isActive).length },
    ...eventTypes.map(t => ({ id: t, label: t, count: events.filter(e => e.eventType === t).length })),
    { id: 'face-search', label: 'Face Search',  count: events.filter(e => e.enableFaceRecognition).length },
    { id: 'gallery',     label: 'Gallery',      count: events.filter(e => e.allowGalleryBrowsing).length },
  ];

  return (
    <div className="scrollbar-none flex gap-2 overflow-x-auto pb-0.5">
      {chips.map((chip) => (
        <button
          key={chip.id}
          type="button"
          onClick={() => onFilterChange(chip.id)}
          className={[
            'flex shrink-0 items-center gap-1.5 rounded-full px-4 py-1.5 text-xs font-medium transition-all duration-200',
            activeFilter === chip.id
              ? 'bg-indigo-600 text-white shadow-lg shadow-indigo-500/25'
              : 'bg-slate-800 text-slate-400 hover:bg-slate-700 hover:text-slate-200',
          ].join(' ')}
        >
          {chip.label}
          {chip.count > 0 ? (
            <span
              className={[
                'rounded-full px-1.5 py-px text-[10px] font-bold',
                activeFilter === chip.id
                  ? 'bg-white/20 text-white'
                  : 'bg-slate-700 text-slate-400',
              ].join(' ')}
            >
              {chip.count}
            </span>
          ) : null}
        </button>
      ))}
    </div>
  );
}
