import { Camera, Plus } from 'lucide-react';
import { Link } from 'react-router-dom';
import { motion } from 'framer-motion';

interface EmptyEventsStateProps {
  hasSearch?: boolean;
}

export function EmptyEventsState({ hasSearch = false }: EmptyEventsStateProps) {
  return (
    <motion.div
      initial={{ opacity: 0, y: 20 }}
      animate={{ opacity: 1, y: 0 }}
      transition={{ duration: 0.4 }}
      className="flex flex-col items-center justify-center rounded-2xl border border-dashed border-slate-700 bg-slate-900/40 py-24 text-center"
    >
      {/* Icon */}
      <div className="relative mb-6">
        <div className="flex h-24 w-24 items-center justify-center rounded-2xl bg-gradient-to-br from-indigo-900/60 to-violet-900/60 ring-1 ring-slate-700">
          <Camera className="h-12 w-12 text-indigo-400" />
        </div>
        {!hasSearch ? (
          <div className="absolute -right-2 -top-2 flex h-7 w-7 items-center justify-center rounded-full bg-violet-600 text-white shadow-lg shadow-violet-500/40">
            <Plus className="h-4 w-4" />
          </div>
        ) : null}
      </div>

      <h3 className="text-xl font-semibold text-slate-200">
        {hasSearch ? 'No events match your search' : 'No events created yet'}
      </h3>
      <p className="mt-2 max-w-sm text-sm text-slate-500">
        {hasSearch
          ? "Try adjusting your search term or selecting a different filter."
          : "Create your first photography event and start sharing moments with your guests."}
      </p>

      {!hasSearch ? (
        <Link
          to="/admin/events/new"
          className="mt-8 inline-flex items-center gap-2 rounded-xl bg-gradient-to-r from-indigo-600 to-violet-600 px-6 py-3 text-sm font-semibold text-white shadow-lg shadow-indigo-500/25 transition-all hover:from-indigo-500 hover:to-violet-500 hover:shadow-indigo-500/40"
        >
          <Plus className="h-4 w-4" />
          Create Your First Event
        </Link>
      ) : null}
    </motion.div>
  );
}
