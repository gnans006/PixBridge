import { useRef, useState, useCallback, type DragEvent, type ChangeEvent } from 'react';
import { motion, AnimatePresence } from 'framer-motion';
import { Camera, ImagePlus, X } from 'lucide-react';

interface EventCoverUploaderProps {
  value: string | null;
  onChange: (url: string | null) => void;
}

export function EventCoverUploader({ value, onChange }: EventCoverUploaderProps) {
  const inputRef = useRef<HTMLInputElement>(null);
  const [isDragging, setIsDragging] = useState(false);

  const handleFile = useCallback(
    (file: File) => {
      if (!file.type.startsWith('image/')) return;
      // Revoke previous object URL to avoid memory leaks
      if (value?.startsWith('blob:')) URL.revokeObjectURL(value);
      onChange(URL.createObjectURL(file));
    },
    [value, onChange],
  );

  const handleDrop = (e: DragEvent<HTMLDivElement>) => {
    e.preventDefault();
    setIsDragging(false);
    const file = e.dataTransfer.files[0];
    if (file) handleFile(file);
  };

  const handleInputChange = (e: ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) handleFile(file);
    // Reset so the same file can be re-selected
    e.target.value = '';
  };

  const handleRemove = () => {
    if (value?.startsWith('blob:')) URL.revokeObjectURL(value);
    onChange(null);
  };

  return (
    <div className="relative h-full min-h-[176px]">
      <AnimatePresence mode="wait">
        {value ? (
          <motion.div
            key="preview"
            initial={{ opacity: 0, scale: 0.96 }}
            animate={{ opacity: 1, scale: 1 }}
            exit={{ opacity: 0, scale: 0.96 }}
            transition={{ duration: 0.18 }}
            className="relative h-full overflow-hidden rounded-xl"
          >
            <img
              src={value}
              alt="Event cover preview"
              className="h-full w-full object-cover"
              style={{ minHeight: 176 }}
            />
            {/* Gradient overlay */}
            <div className="pointer-events-none absolute inset-0 bg-gradient-to-t from-black/70 via-black/10 to-transparent" />
            {/* Remove button */}
            <button
              type="button"
              onClick={handleRemove}
              aria-label="Remove cover image"
              className="absolute right-2 top-2 rounded-full bg-black/60 p-1.5 text-white backdrop-blur-sm transition-colors hover:bg-black/80"
            >
              <X className="h-3.5 w-3.5" />
            </button>
            {/* Replace button */}
            <button
              type="button"
              onClick={() => inputRef.current?.click()}
              className="absolute bottom-2 left-2 flex items-center gap-1.5 rounded-lg bg-black/60 px-3 py-1.5 text-xs font-medium text-white backdrop-blur-sm transition-colors hover:bg-black/80"
            >
              <Camera className="h-3 w-3" />
              Replace
            </button>
          </motion.div>
        ) : (
          <motion.div
            key="upload"
            initial={{ opacity: 0 }}
            animate={{ opacity: 1 }}
            exit={{ opacity: 0 }}
            transition={{ duration: 0.15 }}
            className={`flex h-full cursor-pointer select-none flex-col items-center justify-center gap-3 rounded-xl border-2 border-dashed p-6 text-center transition-colors ${
              isDragging
                ? 'border-indigo-500 bg-indigo-500/10'
                : 'border-slate-700 bg-slate-800/40 hover:border-slate-600 hover:bg-slate-800/60'
            }`}
            style={{ minHeight: 176 }}
            onClick={() => inputRef.current?.click()}
            onDragOver={(e) => {
              e.preventDefault();
              setIsDragging(true);
            }}
            onDragLeave={() => setIsDragging(false)}
            onDrop={handleDrop}
            role="button"
            tabIndex={0}
            aria-label="Upload event cover image — drag and drop or click to browse"
            onKeyDown={(e) => {
              if (e.key === 'Enter' || e.key === ' ') {
                e.preventDefault();
                inputRef.current?.click();
              }
            }}
          >
            <motion.div
              animate={isDragging ? { scale: 1.1 } : { scale: 1 }}
              className="flex h-12 w-12 items-center justify-center rounded-xl bg-slate-700 text-slate-400"
            >
              <ImagePlus className="h-5 w-5" />
            </motion.div>
            <div>
              <p className="text-sm font-medium text-slate-300">Event Cover</p>
              <p className="mt-0.5 text-xs text-slate-500">
                {isDragging ? 'Drop image here' : 'Drag & drop or click to upload'}
              </p>
              <p className="mt-1 text-[11px] text-slate-600">JPG, PNG, WebP</p>
            </div>
          </motion.div>
        )}
      </AnimatePresence>

      <input
        ref={inputRef}
        type="file"
        accept="image/jpeg,image/png,image/webp"
        className="sr-only"
        aria-hidden="true"
        tabIndex={-1}
        onChange={handleInputChange}
      />
    </div>
  );
}
