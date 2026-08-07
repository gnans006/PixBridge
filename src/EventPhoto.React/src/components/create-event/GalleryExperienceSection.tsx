import { Layers } from 'lucide-react';
import { SectionCard } from './SectionCard';
import { GalleryModeCards, type GalleryMode } from './GalleryModeCards';

interface GalleryExperienceSectionProps {
  value: GalleryMode;
  onChange: (mode: GalleryMode) => void;
  error?: string;
}

export function GalleryExperienceSection({ value, onChange, error }: GalleryExperienceSectionProps) {
  return (
    <SectionCard
      icon={<Layers className="h-4 w-4" />}
      title="Gallery Experience"
      subtitle="How guests access their photos"
      defaultOpen
    >
      <GalleryModeCards value={value} onChange={onChange} error={error} />
    </SectionCard>
  );
}
