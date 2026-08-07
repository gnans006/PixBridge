import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { motion, AnimatePresence } from 'framer-motion';
import { AlertTriangle, Brain, CheckCircle, Loader2, RefreshCw, RotateCcw, Save, XCircle } from 'lucide-react';
import toast from 'react-hot-toast';
import { workspaceApi } from '../../api/workspace';
import { apiError } from '../../utils/errorHandler';
import type { EventWorkspaceResponse, UpdateFaceRecognitionSettingsRequest } from '../../types';
import { Modal } from '../UI/Modal';
import { Button } from '../UI/Button';

interface EventFaceRecognitionTabProps {
  workspace: EventWorkspaceResponse;
}

export function EventFaceRecognitionTab({ workspace }: EventFaceRecognitionTabProps) {
  const qc = useQueryClient();
  const [dirty, setDirty] = useState(false);
  const [showEnableWarning, setShowEnableWarning] = useState(false);
  const [pendingEnable, setPendingEnable] = useState<UpdateFaceRecognitionSettingsRequest | null>(null);

  // Face metrics
  const { data: metricsData, isLoading: metricsLoading } = useQuery({
    queryKey: ['face-metrics', workspace.id],
    queryFn: () => workspaceApi.getFaceMetrics(workspace.id),
    enabled: workspace.enableFaceRecognition,
  });
  const metrics = metricsData?.data;

  const defaults: UpdateFaceRecognitionSettingsRequest = {
    enableFaceRecognition: workspace.enableFaceRecognition,
    faceMatchThreshold: workspace.faceMatchThreshold,
    allowFaceSearch: workspace.allowFaceSearch,
  };

  const { register, handleSubmit, reset, watch, setValue } = useForm<UpdateFaceRecognitionSettingsRequest>({
    defaultValues: defaults,
  });

  const enableFR = watch('enableFaceRecognition');
  watch(() => setDirty(true));

  const saveMutation = useMutation({
    mutationFn: (data: UpdateFaceRecognitionSettingsRequest) =>
      workspaceApi.updateFaceRecognition(workspace.id, data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['workspace', workspace.id] });
      qc.invalidateQueries({ queryKey: ['face-metrics', workspace.id] });
      qc.invalidateQueries({ queryKey: ['events'] });
      toast.success('Face recognition settings saved.');
      setDirty(false);
    },
    onError: (e) => apiError(e, 'Failed to save face recognition settings.'),
  });

  const rebuildMutation = useMutation({
    mutationFn: () => workspaceApi.rebuildFaceIndex(workspace.id),
    onSuccess: (r) => {
      qc.invalidateQueries({ queryKey: ['face-metrics', workspace.id] });
      toast.success(`Queued ${r.data ?? 0} photos for re-indexing.`);
    },
    onError: (e) => apiError(e, 'Failed to rebuild face index.'),
  });

  const onSubmit = handleSubmit((data) => {
    // Warn when enabling face recognition for a non-empty event
    if (!workspace.enableFaceRecognition && data.enableFaceRecognition && workspace.photoCount > 0) {
      setPendingEnable(data);
      setShowEnableWarning(true);
      return;
    }
    saveMutation.mutate(data);
  });

  const confirmEnable = () => {
    if (pendingEnable) saveMutation.mutate(pendingEnable);
    setShowEnableWarning(false);
    setPendingEnable(null);
  };

  const handleDiscard = () => {
    reset(defaults);
    setDirty(false);
  };

  return (
    <div className="space-y-6">
      {/* Settings card */}
      <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
        <div className="mb-5 flex items-center gap-2">
          <Brain className="h-4 w-4 text-violet-400" />
          <h2 className="text-sm font-semibold text-white">AI Face Recognition</h2>
        </div>

        <form onSubmit={onSubmit} className="space-y-6">
          {/* Enable toggle */}
          <div className="flex items-start justify-between gap-4 rounded-xl border border-slate-700/50 p-4 hover:border-slate-600">
            <div>
              <p className="text-sm font-semibold text-white">Enable Face Recognition</p>
              <p className="mt-0.5 text-xs text-slate-400">
                The background indexer will process photos and enable AI-powered face search.
              </p>
            </div>
            <Toggle
              checked={enableFR}
              onChange={(v) => { setValue('enableFaceRecognition', v, { shouldDirty: true }); if (!v) setValue('allowFaceSearch', false, { shouldDirty: true }); setDirty(true); }}
            />
          </div>

          <AnimatePresence>
            {enableFR && (
              <motion.div
                initial={{ opacity: 0, height: 0 }}
                animate={{ opacity: 1, height: 'auto' }}
                exit={{ opacity: 0, height: 0 }}
                className="overflow-hidden"
              >
                <div className="space-y-5 pt-2">
                  {/* Allow face search */}
                  <div className="flex items-start justify-between gap-4 rounded-xl border border-slate-700/50 p-4 hover:border-slate-600">
                    <div>
                      <p className="text-sm font-semibold text-white">Allow Guest Face Search</p>
                      <p className="mt-0.5 text-xs text-slate-400">Guests can upload a selfie to find their matched photos.</p>
                    </div>
                    <Toggle
                      checked={watch('allowFaceSearch')}
                      onChange={(v) => { setValue('allowFaceSearch', v, { shouldDirty: true }); setDirty(true); }}
                    />
                  </div>

                  {/* Threshold slider */}
                  <div className="space-y-3">
                    <label className="block text-xs font-semibold uppercase tracking-wider text-slate-400">
                      Match Threshold — <span className="font-bold text-violet-400">{(watch('faceMatchThreshold') * 100).toFixed(0)}%</span>
                    </label>
                    <input
                      {...register('faceMatchThreshold', { valueAsNumber: true, min: 0.3, max: 1.0 })}
                      type="range"
                      min={0.3}
                      max={1.0}
                      step={0.01}
                      className="w-full accent-violet-500"
                    />
                    <div className="flex justify-between text-xs text-slate-500">
                      <span>More results (30%)</span>
                      <span>Recommended (75%)</span>
                      <span>Strict (100%)</span>
                    </div>
                  </div>
                </div>
              </motion.div>
            )}
          </AnimatePresence>

          <AnimatePresence>
            {dirty && (
              <motion.div
                initial={{ opacity: 0, y: 4 }}
                animate={{ opacity: 1, y: 0 }}
                exit={{ opacity: 0, y: 4 }}
                className="flex items-center justify-end gap-3 border-t border-slate-800 pt-4"
              >
                <button type="button" onClick={handleDiscard} disabled={saveMutation.isPending} className="inline-flex items-center gap-2 rounded-xl px-4 py-2 text-sm font-medium text-slate-400 hover:text-white disabled:opacity-50">
                  <RotateCcw className="h-3.5 w-3.5" /> Discard
                </button>
                <button type="submit" disabled={saveMutation.isPending} className="inline-flex items-center gap-2 rounded-xl bg-violet-600 px-5 py-2 text-sm font-semibold text-white hover:bg-violet-500 disabled:opacity-60">
                  {saveMutation.isPending ? <><Loader2 className="h-4 w-4 animate-spin" /> Saving…</> : <><Save className="h-4 w-4" /> Save Settings</>}
                </button>
              </motion.div>
            )}
          </AnimatePresence>
        </form>
      </div>

      {/* Metrics card (only when FR enabled) */}
      {workspace.enableFaceRecognition && (
        <div className="rounded-2xl border border-slate-800 bg-slate-900 p-6">
          <div className="mb-5 flex items-center justify-between">
            <h2 className="text-sm font-semibold text-white">Indexing Metrics</h2>
            <button
              onClick={() => rebuildMutation.mutate()}
              disabled={rebuildMutation.isPending}
              className="inline-flex items-center gap-1.5 rounded-xl border border-slate-700 px-3 py-1.5 text-xs font-medium text-slate-300 transition-colors hover:border-violet-500 hover:text-white disabled:opacity-50"
            >
              {rebuildMutation.isPending ? <Loader2 className="h-3.5 w-3.5 animate-spin" /> : <RefreshCw className="h-3.5 w-3.5" />}
              Rebuild Index
            </button>
          </div>

          {metricsLoading ? (
            <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
              {Array.from({ length: 4 }).map((_, i) => (
                <div key={i} className="h-16 animate-pulse rounded-xl bg-slate-800" />
              ))}
            </div>
          ) : metrics ? (
            <div className="grid grid-cols-2 gap-4 sm:grid-cols-4">
              <MetricTile label="Total Photos" value={metrics.totalPhotos} color="text-slate-300" />
              <MetricTile label="Indexed Faces" value={metrics.indexedFaces} color="text-violet-400" />
              <MetricTile label="Indexed Photos" value={metrics.indexedPhotos} color="text-emerald-400" />
              <MetricTile label="Pending" value={metrics.pendingPhotos} color="text-amber-400" />
            </div>
          ) : null}
        </div>
      )}

      {/* Enable warning modal */}
      <Modal isOpen={showEnableWarning} onClose={() => setShowEnableWarning(false)} title="Enable Face Recognition">
        <div className="flex gap-3">
          <AlertTriangle className="mt-0.5 h-5 w-5 shrink-0 text-amber-400" />
          <div className="space-y-2">
            <p className="text-sm text-slate-300">
              This event contains <strong className="text-white">{workspace.photoCount.toLocaleString()}</strong> photos.
            </p>
            <p className="text-sm text-slate-300">
              Face indexing will start in the background. Estimated faces:{' '}
              <strong className="text-white">{(workspace.photoCount * 2).toLocaleString()}+</strong>
            </p>
            <p className="text-xs text-slate-400">
              Indexing runs incrementally and will not impact gallery performance.
            </p>
          </div>
        </div>
        <div className="mt-5 flex justify-end gap-3">
          <Button variant="secondary" onClick={() => setShowEnableWarning(false)}>Cancel</Button>
          <Button variant="primary" onClick={confirmEnable} disabled={saveMutation.isPending}>
            {saveMutation.isPending ? 'Enabling…' : 'Continue'}
          </Button>
        </div>
      </Modal>
    </div>
  );
}

function Toggle({ checked, onChange }: { checked: boolean; onChange: (v: boolean) => void }) {
  return (
    <button
      type="button"
      onClick={() => onChange(!checked)}
      className={`relative inline-flex h-6 w-11 shrink-0 cursor-pointer rounded-full border-2 border-transparent transition-colors duration-200 ${checked ? 'bg-violet-600' : 'bg-slate-700'}`}
    >
      <span className={`pointer-events-none inline-block h-5 w-5 transform rounded-full bg-white shadow transition duration-200 ${checked ? 'translate-x-5' : 'translate-x-0'}`} />
    </button>
  );
}

function MetricTile({ label, value, color }: { label: string; value: number; color: string }) {
  return (
    <div className="rounded-xl border border-slate-700/50 bg-slate-800/50 p-4 text-center">
      <p className={`text-2xl font-bold ${color}`}>{value.toLocaleString()}</p>
      <p className="mt-1 text-xs text-slate-400">{label}</p>
    </div>
  );
}
